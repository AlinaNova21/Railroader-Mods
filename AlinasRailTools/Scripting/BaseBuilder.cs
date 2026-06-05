using Serilog;

namespace AlinasRailTools.Scripting;

public abstract class BaseBuilder<TParent, TBuilder>(TParent parent, string id)
  where TBuilder : BaseBuilder<TParent, TBuilder>
{
  protected readonly ILogger Logger = Log.ForContext<TBuilder>().ForContext("Id", id);
  public string Id => id;
  protected TParent Parent => parent;
  public TParent Done() => parent;
  public TParent Done(bool setActive)
  {
    if (setActive)
    {
      SetActive(true);
    }
    return parent;
  }

  public virtual TBuilder SetActive(bool active = true)
  {
    // Default implementation - subclasses should override if they have a GameObject to activate
    return (TBuilder)this;
  }
  public abstract TParent Remove();
  public virtual TBuilder Invalidate() => (TBuilder)this;
  public virtual TBuilder Clone() => throw new System.NotImplementedException($"Clone not implemented for {typeof(TBuilder).Name}");
}
