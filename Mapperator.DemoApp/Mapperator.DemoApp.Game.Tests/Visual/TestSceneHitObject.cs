using Mapperator.DemoApp.Game.Drawables;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding.HitObjects;
using osu.Framework.Graphics;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Tests.Visual
{
    [TestFixture]
    public class TestSceneHitObject : DemoAppTestScene
    {
        private DrawableHitObject hitObject;

        public TestSceneHitObject()
        {
            var decoder = new HitObjectDecoder();
            Add(new DrawableHitObject(decoder.Decode(@"133,324,42970,1,4,2:3:0:0:")));
            Add(new DrawableHitObject(decoder.Decode(@"0,223,42794,2,0,P|58:242|70:288,1,59.9999981689454,6|0,2:2|0:0,0:0:0:0:")));
            Add(new DrawableHitObject(decoder.Decode(@"324,321,42529,6,0,B|298:303|298:303|197:318,1,119.999996337891,2|0,2:2|0:0,0:0:0:0:")));
            Add(hitObject = new DrawableHitObject(decoder.Decode(@"140,191,41823,6,0,B|266:216|350:152|287:73|360:40|478:61|478:61|414:72|397:123,1,480,6|0,1:2|0:0,0:0:0:0:")));
            Add(new DrawableHitObject(decoder.Decode(@"365,198,41647,2,0,P|389:234|389:266,1,59.9999981689454,2|0,2:2|0:0,0:0:0:0:")));
            Add(new DrawableHitObject(decoder.Decode(@"401,363,41470,2,0,P|358:370|329:356,1,59.9999981689454")));
            Add(new DrawableHitObject(decoder.Decode(@"483,384,41117,6,0,B|494:297|494:297|464:325,1,119.999996337891,2|2,2:2|2:2,0:0:0:0:")));
            Add(new DrawableHitObject(decoder.Decode(@"64,71,40059,6,0,B|114:12|114:12|106:118|106:118|96:142|96:142|90:277,1,284.999992389679,6|0,2:2|0:0,0:0:0:0:")));
            Add(new DrawableHitObject(decoder.Decode(@"191,100,39706,6,0,L|225:122,6,25,10|8|8|8|10|8|8,2:2|2:2|2:2|2:2|2:2|2:2|2:2,0:0:0:0:")));
        }
    }
}
