// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Drawables
{
    public abstract class SliderBody : CompositeDrawable
    {
        private DrawableSliderPath path;

        protected Path Path => path;

        public virtual float PathRadius
        {
            get => path.PathRadius;
            set
            {
                if (path.PathRadius == value)
                    return;

                path.PathRadius = value;
                repositionSliderPath();
            }
        }

        /// <summary>
        /// Offset in absolute coordinates from the start of the curve.
        /// </summary>
        public virtual Vector2 PathOffset => path.PositionInBoundingBox(path.Vertices[0]);

        /// <summary>
        /// Used to colour the path.
        /// </summary>
        public Color4 AccentColour
        {
            get => path.AccentColour;
            set
            {
                if (path.AccentColour == value)
                    return;

                path.AccentColour = value;
            }
        }

        /// <summary>
        /// Used to colour the path border.
        /// </summary>
        public new Color4 BorderColour
        {
            get => path.BorderColour;
            set
            {
                if (path.BorderColour == value)
                    return;

                path.BorderColour = value;
            }
        }

        /// <summary>
        /// Used to size the path border.
        /// </summary>
        public float BorderSize
        {
            get => path.BorderSize;
            set
            {
                if (path.BorderSize == value)
                    return;

                path.BorderSize = value;
                repositionSliderPath();
            }
        }

        protected SliderBody()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            RecyclePath();
        }

        /// <summary>
        /// Initialises a new <see cref="DrawableSliderPath"/>, releasing all resources retained by the old one.
        /// </summary>
        public virtual void RecyclePath()
        {
            InternalChild = path = CreateSliderPath().With(p =>
            {
                p.Position = path?.Position ?? Vector2.Zero;
                p.PathRadius = path?.PathRadius ?? 10;
                p.AccentColour = path?.AccentColour ?? Color4.White;
                p.BorderColour = path?.BorderColour ?? Color4.White;
                p.BorderSize = path?.BorderSize ?? 1;
                p.Vertices = path?.Vertices ?? Array.Empty<Vector2>();
                p.Anchor = Anchor.Centre;
            });
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => path.ReceivePositionalInputAt(screenSpacePos);

        /// <summary>
        /// Sets the vertices of the path which should be drawn by this <see cref="SliderBody"/>.
        /// </summary>
        /// <param name="vertices">The vertices</param>
        protected void SetVertices(IReadOnlyList<Vector2> vertices)
        {
            path.Vertices = vertices;
            repositionSliderPath();
        }

        private void repositionSliderPath()
        {
            if (path.Vertices.Count > 0)
            {
                path.Position = -PathOffset;
            }
        }

        protected virtual DrawableSliderPath CreateSliderPath() => new DefaultDrawableSliderPath();

        private class DefaultDrawableSliderPath : DrawableSliderPath
        {
            private const float opacity_at_centre = 0.3f;
            private const float opacity_at_edge = 0.8f;

            protected override Color4 ColourAt(float position)
            {
                if (CalculatedBorderPortion != 0f && position <= CalculatedBorderPortion)
                    return BorderColour;

                position -= CalculatedBorderPortion;
                return new Color4(AccentColour.R, AccentColour.G, AccentColour.B, (opacity_at_edge - (opacity_at_edge - opacity_at_centre) * position / GRADIENT_PORTION) * AccentColour.A);
            }
        }
    }
}
