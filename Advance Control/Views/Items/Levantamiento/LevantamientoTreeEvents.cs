using System;

namespace Advance_Control.Views.Items.Levantamiento
{
    public sealed class LevantamientoTreeItemActionRequestedEventArgs : EventArgs
    {
        public string HotspotKey { get; init; } = string.Empty;
    }
}
