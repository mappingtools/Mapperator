// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace Mapperator.DemoApp.Game.Drawables
{
    public partial class NumberPiece : Container
    {
        private readonly SpriteText number;

        public string Text
        {
            get => number.Text.ToString();
            set => number.Text = value;
        }

        public NumberPiece()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Children = new Drawable[]
            {
                number = new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.Numeric.With(size: 40),
                    UseFullGlyphHeight = true,
                    Text = @"1"
                }
            };
        }
    }
}
