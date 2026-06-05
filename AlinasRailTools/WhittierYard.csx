using AlinasRailTools.Scripting;
using UnityEngine;

class WhittierYard
{
    public void CreateWhittierYard()
    {
        var zone = "AN_Whittier_Yard";
        var helper = new Helper(zone);

        // Get external node references
        var n5es = helper.GetTrackNode("N5es").WithFlipSwitchStand(true);
        var nijv = helper.GetTrackNode("Nijv");

        // Create track nodes and store references

        var yard00 = helper.CreateTrackNode("NAN_Whittier_Yard_00")
            .At(13240f, 561.46f, 4373f)
            .WithRotation(0f, 305f, 0f);

        var yard01 = helper.CreateTrackNode("NAN_Whittier_Yard_01")
            .At(13271.384658498273f, 561.46f, 4339.63854101625f)
            .WithRotation(0f, 115.81f, 0f)
            .WithFlipSwitchStand(true);

        var yard02 = helper.CreateTrackNode("NAN_Whittier_Yard_02")
            .At(13274.572252705657f, 561.46f, 4341.188812823539f)
            .WithRotation(0f, 308.81f, 0f);

        var t0_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T0_00")
            .At(13208.812723503488f, 561.25f, 4396.033307171189f)
            .WithRotation(0f, 309f, 0f);

        var t0_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T0_01")
            .At(13160.401952767694f, 561.25f, 4434.700511633659f)
            .WithRotation(0f, 309f, 0f);

        var t0_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T0_02")
            .At(12958.344002788881f, 561.25f, 4598.323813306617f)
            .WithRotation(0f, 309f, 0f);

        var t0_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T0_03")
            .At(12916.566086040342f, 561.25f, 4628.944514945485f)
            .WithRotation(0f, 297f, 0f);

        var t1_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T1_00")
            .At(13184.359739032465f, 561.25f, 4413.159619989581f)
            .WithRotation(0f, 297f, 0f);

        var t1_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T1_01")
            .At(13142.581822283926f, 561.25f, 4443.780321628449f)
            .WithRotation(0f, 309f, 0f);

        var t1_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T1_02")
            .At(12940.523872305113f, 561.25f, 4607.403623301408f)
            .WithRotation(0f, 309f, 0f);

        var t1_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T1_03")
            .At(12898.745955556575f, 561.25f, 4638.024324940276f)
            .WithRotation(0f, 297f, 0f);

        var t2_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T2_00")
            .At(13166.539608548697f, 561.25f, 4422.239429984372f)
            .WithRotation(0f, 297f, 0f);

        var t2_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T2_01")
            .At(13124.761691800159f, 561.25f, 4452.86013162324f)
            .WithRotation(0f, 309f, 0f);

        var t2_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T2_02")
            .At(12922.703741821346f, 561.25f, 4616.483433296198f)
            .WithRotation(0f, 309f, 0f);

        var t2_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T2_03")
            .At(12880.925825072807f, 561.25f, 4647.104134935066f)
            .WithRotation(0f, 297f, 0f);

        var t3_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T3_00")
            .At(13148.71947806493f, 561.25f, 4431.319239979162f)
            .WithRotation(0f, 297f, 0f);

        var t3_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T3_01")
            .At(13106.941561316391f, 561.25f, 4461.93994161803f)
            .WithRotation(0f, 309f, 0f);

        var t3_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T3_02")
            .At(12904.883611337578f, 561.25f, 4625.563243290989f)
            .WithRotation(0f, 309f, 0f);

        var t3_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T3_03")
            .At(12863.10569458904f, 561.25f, 4656.183944929857f)
            .WithRotation(0f, 297f, 0f);

        var t4_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T4_00")
            .At(13130.899347581162f, 561.25f, 4440.399049973953f)
            .WithRotation(0f, 297f, 0f);

        var t4_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T4_01")
            .At(13089.121430832623f, 561.25f, 4471.019751612821f)
            .WithRotation(0f, 309f, 0f);

        var t4_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T4_02")
            .At(12887.06348085381f, 561.25f, 4634.643053285779f)
            .WithRotation(0f, 309f, 0f);

        var t4_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T4_03")
            .At(12845.285564105272f, 561.25f, 4665.263754924647f)
            .WithRotation(0f, 297f, 0f);

        var t5_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T5_00")
            .At(13113.079217097395f, 561.25f, 4449.478859968744f)
            .WithRotation(0f, 297f, 0f);

        var t5_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T5_01")
            .At(13071.301300348856f, 561.25f, 4480.099561607612f)
            .WithRotation(0f, 309f, 0f);

        var t5_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T5_02")
            .At(12869.243350370043f, 561.25f, 4643.72286328057f)
            .WithRotation(0f, 309f, 0f);

        var t5_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T5_03")
            .At(12827.465433621504f, 561.25f, 4674.343564919438f)
            .WithRotation(0f, 297f, 0f);

        var t6_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T6_00")
            .At(13095.259086613627f, 561.25f, 4458.558669963534f)
            .WithRotation(0f, 297f, 0f);

        var t6_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T6_01")
            .At(13053.481169865088f, 561.25f, 4489.179371602402f)
            .WithRotation(0f, 309f, 0f);

        var t6_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T6_02")
            .At(12851.423219886276f, 561.25f, 4652.802673275361f)
            .WithRotation(0f, 309f, 0f);

        var t6_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T6_03")
            .At(12809.645303137737f, 561.25f, 4683.423374914229f)
            .WithRotation(0f, 297f, 0f);

        var t7_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T7_00")
            .At(13077.43895612986f, 561.25f, 4467.638479958325f)
            .WithRotation(0f, 297f, 0f);

        var t7_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T7_01")
            .At(13035.66103938132f, 561.25f, 4498.259181597193f)
            .WithRotation(0f, 309f, 0f);

        var t7_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T7_02")
            .At(12833.603089402508f, 561.25f, 4661.882483270151f)
            .WithRotation(0f, 309f, 0f);

        var t7_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T7_03")
            .At(12791.82517265397f, 561.25f, 4692.503184909019f)
            .WithRotation(0f, 297f, 0f);

        var t8_00 = helper.CreateTrackNode("NAN_Whittier_Yard_T8_00")
            .At(13059.618825646092f, 561.25f, 4476.718289953115f)
            .WithRotation(0f, 297f, 0f);

        var t8_01 = helper.CreateTrackNode("NAN_Whittier_Yard_T8_01")
            .At(13017.840908897553f, 561.25f, 4507.338991591983f)
            .WithRotation(0f, 309f, 0f);

        var t8_02 = helper.CreateTrackNode("NAN_Whittier_Yard_T8_02")
            .At(12815.78295891874f, 561.25f, 4670.962293264942f)
            .WithRotation(0f, 309f, 0f);

        var t8_03 = helper.CreateTrackNode("NAN_Whittier_Yard_T8_03")
            .At(12774.005042170202f, 561.25f, 4701.58299490381f)
            .WithRotation(0f, 297f, 0f);

        var yard03 = helper.CreateTrackNode("NAN_Whittier_Yard_03")
            .At(13264.871127176651f, 561.46f, 4356.224212895878f)
            .WithRotation(0f, 302f, 0f);

        var yard04 = helper.CreateTrackNode("NAN_Whittier_Yard_04")
            .At(13395.57238688626f, 561.46f, 4306.052699959536f)
            .WithRotation(0f, 280f, 0f);

        var yard05 = helper.CreateTrackNode("NAN_Whittier_Yard_05")
            .At(12668.819653211203f, 559.6f, 4768.0629799750395f)
            .WithRotation(0f, 126.406487f, 0f)
            .WithFlipSwitchStand(true);

        var yard06 = helper.CreateTrackNode("NAN_Whittier_Yard_06")
            .At(12692.96445137928f, 559.6f, 4750.257679600631f)
            .WithRotation(0f, 126.406487f, 0f);

        var yard07 = helper.CreateTrackNode("NAN_Whittier_Yard_07")
            .At(12694.424407311712f, 559.6f, 4752.429791961954f)
            .WithRotation(0f, 121.406487f, 0f);

        // Create track segments
        // Main connections (no group by default)
        helper.CreateTrackSegment("SAN_Whittier_Yard_00", n5es.Node, yard01.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_05", nijv.Node, yard05.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_06", yard05.Node, yard06.Node);

        // Yard lead segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Lead");
        helper.CreateTrackSegment("SAN_Whittier_Yard_01", n5es.Node, yard02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_02", yard00.Node, yard02.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L0_00", yard00.Node, t0_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L1_00", t0_00.Node, t1_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L2_00", t1_00.Node, t2_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L3_00", t2_00.Node, t3_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L4_00", t3_00.Node, t4_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L5_00", t4_00.Node, t5_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L6_00", t5_00.Node, t6_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L7_00", t6_00.Node, t7_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L8_00", t7_00.Node, t8_00.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_03", yard00.Node, yard03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_04", yard03.Node, yard04.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_07", yard05.Node, yard07.Node);
        // Lead connections at the back of the yard
        helper.CreateTrackSegment("SAN_Whittier_Yard_L0_01", t0_03.Node, t1_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L1_01", t1_03.Node, t2_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L2_01", t2_03.Node, t3_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L3_01", t3_03.Node, t4_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L4_01", t4_03.Node, t5_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L5_01", t5_03.Node, t6_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L6_01", t6_03.Node, t7_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L7_01", t7_03.Node, t8_03.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_L8_01", t8_03.Node, yard07.Node);

        // Track 0 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_0");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T0_00", t0_00.Node, t0_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T0_01", t0_01.Node, t0_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T0_02", t0_02.Node, t0_03.Node).WithPriority(-1);

        // Track 1 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_1");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T1_00", t1_00.Node, t1_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T1_01", t1_01.Node, t1_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T1_02", t1_02.Node, t1_03.Node).WithPriority(-1);

        // Track 2 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_2");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T2_00", t2_00.Node, t2_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T2_01", t2_01.Node, t2_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T2_02", t2_02.Node, t2_03.Node).WithPriority(-1);

        // Track 3 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_3");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T3_00", t3_00.Node, t3_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T3_01", t3_01.Node, t3_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T3_02", t3_02.Node, t3_03.Node).WithPriority(-1);

        // Track 4 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_4");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T4_00", t4_00.Node, t4_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T4_01", t4_01.Node, t4_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T4_02", t4_02.Node, t4_03.Node).WithPriority(-1);

        // Track 5 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_5");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T5_00", t5_00.Node, t5_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T5_01", t5_01.Node, t5_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T5_02", t5_02.Node, t5_03.Node).WithPriority(-1);

        // Track 6 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_6");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T6_00", t6_00.Node, t6_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T6_01", t6_01.Node, t6_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T6_02", t6_02.Node, t6_03.Node).WithPriority(-1);

        // Track 7 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_7");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T7_00", t7_00.Node, t7_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T7_01", t7_01.Node, t7_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T7_02", t7_02.Node, t7_03.Node).WithPriority(-1);

        // Track 8 segments
        helper.WithGroupId("AN_Whittier_Yard_Yard_Track_8");
        helper.CreateTrackSegment("SAN_Whittier_Yard_T8_00", t8_00.Node, t8_01.Node).WithPriority(-1);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T8_01", t8_01.Node, t8_02.Node);
        helper.CreateTrackSegment("SAN_Whittier_Yard_T8_02", t8_02.Node, t8_03.Node).WithPriority(-1);
    }
}

// Execute the script
var whittierYard = new WhittierYard();
whittierYard.CreateWhittierYard();