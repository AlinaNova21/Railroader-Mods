using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.State;
using Model.Definition.Data;
using Model.Ops;
using Serilog;
using Track;
using Track.Search;
using UnityEngine;

/// <summary>
/// TeleportingCaptiveLoadingIndustry - Based on TeleportLoadingIndustry but keeps loaded cars captive
/// The key difference is that it does NOT call OrderAwayLoaded, keeping cars at the industry
/// </summary>
public class TeleportingCaptiveLoadingIndustry : IndustryLoaderBase
{
    [Tooltip("Game seconds between loading cars.")]
    public float carLoadPeriod = 600f;

    public TrackSpan[] inputSpans;
    public TrackSpan[] outputSpans;
    public float carLengthFeet = 50f;

    private Graph _graph;
    private TrainController _trainController;
    private readonly List<Vector3> _routePoints = new List<Vector3>();
    private const string LastLoadedKey = "lastLoaded";

    public override bool WantsAutoDestination(AutoDestinationType type)
    {
        return type == AutoDestinationType.Empty && !this.orderEmpties;
    }

    public override void Service(IIndustryContext ctx)
    {
        float contractMultiplier = base.Industry.GetContractMultiplier();
        float num = this.productionRate * contractMultiplier;
        ctx.AddToStorage(this.load, IndustryComponent.RateToValue(num, ctx.DeltaTime), this.maxStorage);
        float num2 = ctx.QuantityInStorage(this.load);
        if (this._trainController == null)
        {
            this._trainController = TrainController.Shared;
        }
        if (this._graph == null)
        {
            this._graph = this._trainController.graph;
        }
        GameDateTime now = ctx.Now;
        GameDateTime dateTime = ctx.GetDateTime("lastLoaded", default(GameDateTime));
        double num3 = now - dateTime;
        if (num3 < (double)this.carLoadPeriod)
        {
            return;
        }
        while (num3 > (double)this.carLoadPeriod)
        {
            float num4 = this.TeleportLoadOneCar(ctx, num2);
            if (num4 < this.load.ZeroThreshold)
            {
                break;
            }
            num2 -= num4;
            num3 -= (double)this.carLoadPeriod;
        }
        ctx.SetDateTime("lastLoaded", now);
    }

    private float TeleportLoadOneCar(IIndustryContext ctx, float qtyAvailableToLoad)
    {
        foreach (TrackSpan trackSpan in this.outputSpans)
        {
            if (qtyAvailableToLoad < this.load.NominalQuantityPerCarLoad)
            {
                break;
            }
            Location location;
            Car car;
            if (this._trainController.FindOpenSpaceFromLower(trackSpan, this.carLengthFeet * 0.3048f, (Car c) => c.EnumerateCoupled(Car.LogicalEnd.A).All(new Func<Car, bool>(this.IsAcceptableAdjacentCutMember)), out location, out car))
            {
                Car car2;
                if (!this.FindEmptyCar(ctx, out car2))
                {
                    return 0f;
                }
                if (car2.QuantityCapacityOfLoad(this.load).Item2 > qtyAvailableToLoad)
                {
                    break;
                }
                using (StateManager.TransactionScope())
                {
                    float num;
                    if (!this.TeleportLoad(car2, location, car, qtyAvailableToLoad, out num))
                    {
                        return 0f;
                    }
                    // KEY DIFFERENCE: Do NOT call OrderAwayLoaded - keep cars captive
                    // ctx.OrderAwayLoaded(new OpsCarAdapter(car2, OpsController.Shared), null, false);
                    ctx.RemoveFromStorage(this.load, num);
                    return num;
                }
            }
        }
        return 0f;
    }

    private bool IsAcceptableAdjacentCutMember(Car car)
    {
        if (Mathf.Abs(car.velocity) < 0.01f && this.carTypeFilter.Matches(car.CarType))
        {
            string text;
            if (car.GetWaybill(OpsController.Shared) == null)
            {
                text = null;
            }
            else
            {
                Waybill? waybill;
                ref Waybill ptr = ref waybill.GetValueOrDefault();
                text = ((ptr.Origin != null) ? ptr.Origin.GetValueOrDefault().Identifier : null);
            }
            return text == base.Identifier;
        }
        return false;
    }

    private bool TeleportLoad(Car car, Location loc, Car adjacentCar, float availableToLoad, out float actuallyLoaded)
    {
        Location location = car.LocationFor(car.ClosestLogicalEndTo(loc, this._graph));
        if (!this.CheckRouteClear(location, loc, new Car[] { car, adjacentCar }))
        {
            actuallyLoaded = 0f;
            return false;
        }
        Car.LogicalEnd logicalEnd = ((car.CoupledTo(Car.LogicalEnd.A) == null) ? Car.LogicalEnd.A : Car.LogicalEnd.B);
        IEnumerable<Car> enumerable = from c in car.EnumerateCoupled(logicalEnd)
            where c != car
            select c;
        this.ApplyHandbrakesToCut(enumerable, false);
        car.SetHandbrake(false);
        if (adjacentCar != null)
        {
            this._trainController.MoveCarCoupleTo(car, loc, adjacentCar);
        }
        else
        {
            this._trainController.MoveCar(car, loc);
        }
        LoadSlot loadSlot = car.Definition.LoadSlots[0];
        float num = global::UnityEngine.Random.Range(0.95f, 1f);
        actuallyLoaded = Mathf.Min(loadSlot.MaximumCapacity, availableToLoad) * num;
        car.SetLoadInfo(0, new CarLoadInfo?(new CarLoadInfo(this.load.id, actuallyLoaded)));
        this.ApplyHandbrakesToCut(car.EnumerateCoupled(Car.End.F), true);
        return true;
    }

    private bool CheckRouteClear(Location start, Location end, Car[] ignoring)
    {
        bool flag;
        try
        {
            this._graph.FindPoints(start, end, 10f, base.name, this._routePoints, null);
            foreach (Vector3 vector in this._routePoints)
            {
                Car car = this._trainController.CheckForCarAtPoint(vector, 1f);
                if (!(car == null) && !ignoring.Contains(car))
                {
                    Log.Information<Car>("Route not clear: found {car}", car);
                    return false;
                }
            }
            flag = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking route");
            flag = false;
        }
        return flag;
    }

    private void ApplyHandbrakesToCut(IEnumerable<Car> cut, bool moved)
    {
        List<Car> list = cut.ToList<Car>();
        int num = list.Count((Car car) => car.air.handbrakeApplied);
        int num2 = Mathf.Max(0, 3 - num);
        foreach (Car car3 in list.Where((Car car) => !car.air.handbrakeApplied).Take(num2))
        {
            car3.SetHandbrake(true);
        }
        if (moved)
        {
            TrainController.ConnectCars(list, true);
            foreach (Car car2 in list)
            {
                if (car2.SupportsBleed() && car2.air.BrakeCylinder.Pressure > 0.1f)
                {
                    car2.SetBleed();
                }
            }
        }
    }

    private bool FindEmptyCar(IIndustryContext ctx, out Car car)
    {
        foreach (TrackSpan trackSpan in this.inputSpans)
        {
            if (this.FindEmptyCar(ctx, trackSpan, out car))
            {
                return true;
            }
        }
        car = null;
        return false;
    }

    private bool FindEmptyCar(IIndustryContext ctx, TrackSpan inputSpan, out Car car)
    {
        HashSet<IOpsCar> hashSet = base.EnumerateCars(ctx, true).ToHashSet<IOpsCar>();
        Car leadCar = this._trainController.CarsOnSpan(inputSpan).FirstOrDefault<Car>();
        if (leadCar == null || hashSet.All((IOpsCar c) => c.Id != leadCar.id) || !this.CanLoadCar(leadCar))
        {
            car = null;
            return false;
        }
        car = leadCar;
        return true;
    }

    private bool CanLoadCar(Car car)
    {
        if (Mathf.Abs(car.velocity) > 0.1f)
        {
            return false;
        }
        if (car.Definition.LoadSlots.Count <= 0 || !car.Definition.LoadSlots[0].LoadRequirementsMatch(this.load))
        {
            return false;
        }
        CarLoadInfo? loadInfo = car.GetLoadInfo(0);
        return loadInfo == null || loadInfo.Value.LoadId == this.load.id || loadInfo.Value.Quantity < 0.1f;
    }
}

Debug.Log("TeleportingCaptiveLoadingIndustry class defined successfully");