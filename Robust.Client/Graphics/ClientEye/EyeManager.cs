﻿using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

#nullable enable

namespace Robust.Client.Graphics.ClientEye
{
    /// <inheritdoc />
    public sealed class EyeManager : IEyeManager
    {
        // If you modify this make sure to edit the value in the Robust.Shared.Audio.AudioParams struct default too!
        // No I can't be bothered to make this a shared constant.
        /// <summary>
        /// Default scaling for the projection matrix.
        /// </summary>
        public const int PixelsPerMeter = 32;

        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        // We default to this when we get set to a null eye.
        private readonly FixedEye _defaultEye = new FixedEye();

        private IEye? _currentEye;

        /// <inheritdoc />
        public IEye CurrentEye
        {
            get => _currentEye ?? _defaultEye;
            set => _currentEye = value;
        }

        public void ClearCurrentEye()
        {
            _currentEye = _defaultEye;
        }

        /// <inheritdoc />
        public MapId CurrentMap => CurrentEye.Position.MapId;

        /// <inheritdoc />
        public Box2 GetWorldViewport()
        {
            var vpSize = _displayManager.ScreenSize;

            var topLeft = ScreenToMap(Vector2.Zero);
            var topRight = ScreenToMap(new Vector2(vpSize.X, 0));
            var bottomRight = ScreenToMap(vpSize);
            var bottomLeft = ScreenToMap(new Vector2(0, vpSize.Y));
            
            var left = MathHelper.Min(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X);
            var bottom = MathHelper.Min(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y);
            var right = MathHelper.Max(topLeft.X, topRight.X, bottomRight.X, bottomLeft.X);
            var top = MathHelper.Max(topLeft.Y, topRight.Y, bottomRight.Y, bottomLeft.Y);

            return new Box2(left, bottom, right, top);
        }

        /// <inheritdoc />
        public Vector2 WorldToScreen(Vector2 point)
        {
            var newPoint = point;

            CurrentEye.GetViewMatrix(out var viewMatrix);
            newPoint = viewMatrix * newPoint;

            // (inlined version of UiProjMatrix)
            newPoint *= new Vector2(1, -1) * PixelsPerMeter;
            // newPoint += _displayManager.ScreenSize / 2f;

            var viewportSize = PixelsPerMeter * 2 * 19;

            newPoint += new Vector2i((int)(_displayManager.ScreenSize.X * 0.5f), (int)(viewportSize * 0.5f));

            return newPoint;
        }

        /// <inheritdoc />
        public void GetScreenProjectionMatrix(out Matrix3 projMatrix)
        {
            Matrix3 result = default;

            result.R0C0 = PixelsPerMeter;
            result.R1C1 = -PixelsPerMeter;

            var screenSize = _displayManager.ScreenSize;
            result.R0C2 = screenSize.X / 2f;
            result.R1C2 = screenSize.Y / 2f;

            result.R2C2 = 1;

            /* column major
             Sx 0 Tx
             0 Sy Ty
             0  0  1
            */
            projMatrix = result;
        }

        /// <inheritdoc />
        public ScreenCoordinates CoordinatesToScreen(EntityCoordinates point)
        {
            return MapToScreen(point.ToMap(_entityManager));
        }

        public ScreenCoordinates MapToScreen(MapCoordinates point)
        {
            return new ScreenCoordinates(WorldToScreen(point.Position));
        }

        /// <inheritdoc />
        public MapCoordinates ScreenToMap(ScreenCoordinates point)
        {
            return ScreenToMap(point.Position);
        }

        /// <inheritdoc />
        public MapCoordinates ScreenToMap(Vector2 point)
        {
            var newPoint = point;
            var viewportSize = PixelsPerMeter * 2 * 19;

            newPoint -= new Vector2i((int)(_displayManager.ScreenSize.X * 0.5f), (int)(viewportSize * 0.5f));

            // (inlined version of UiProjMatrix^-1)
            // newPoint -= _displayManager.ScreenSize / 2f;
            newPoint *= new Vector2(1, -1) / PixelsPerMeter;

            // view matrix
            CurrentEye.GetViewMatrixInv(out var viewMatrixInv);
            newPoint = viewMatrixInv * newPoint;

            return new MapCoordinates(newPoint, CurrentMap);
        }
    }
}
