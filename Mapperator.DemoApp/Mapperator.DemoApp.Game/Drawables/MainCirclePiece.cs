// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace Mapperator.DemoApp.Game.Drawables
{
    public class MainCirclePiece : CompositeDrawable
    {
        private readonly CirclePiece circle;
        private readonly NumberPiece number;

        public MainCirclePiece()
        {
            Size = new Vector2(64 * 2);

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                circle = new CirclePiece(),
                number = new NumberPiece()
            };
        }

        public readonly Bindable<int> IndexInCurrentCombo = new Bindable<int>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            IndexInCurrentCombo.BindValueChanged(index => number.Text = (index.NewValue + 1).ToString(), true);
        }
    }
}
