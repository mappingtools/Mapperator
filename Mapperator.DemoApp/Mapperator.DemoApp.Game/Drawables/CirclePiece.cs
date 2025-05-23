﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace Mapperator.DemoApp.Game.Drawables
{
    public partial class CirclePiece : CompositeDrawable
    {
        public CirclePiece()
        {
            Size = new Vector2(64 * 2);
            Masking = true;

            CornerRadius = Size.X / 2;
            CornerExponent = 2;

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            InternalChild = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = textures.Get("hitcircle3")
            };
        }
    }
}
