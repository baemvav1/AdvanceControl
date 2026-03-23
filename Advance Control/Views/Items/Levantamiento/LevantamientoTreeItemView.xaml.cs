using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Levantamiento
{
    public sealed partial class LevantamientoTreeItemView : UserControl
    {
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(nameof(Item), typeof(LevantamientoTreeItemModel), typeof(LevantamientoTreeItemView), new PropertyMetadata(null));

        public event EventHandler<LevantamientoTreeItemActionRequestedEventArgs>? DeleteRequested;
        public event EventHandler<LevantamientoTreeItemActionRequestedEventArgs>? UploadImageRequested;

        public LevantamientoTreeItemView()
        {
            InitializeComponent();
        }

        public LevantamientoTreeItemModel? Item
        {
            get => (LevantamientoTreeItemModel?)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public Visibility GetLeafVisibility(LevantamientoTreeItemModel? item)
        {
            return item is not null && item.EsHoja
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public string GetDescriptionText(LevantamientoTreeItemModel? item)
        {
            return string.IsNullOrWhiteSpace(item?.DescripcionFalla)
                ? "Sin descripcion capturada."
                : item.DescripcionFalla!;
        }

        private void UploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Item is null || string.IsNullOrWhiteSpace(Item.Clave))
            {
                return;
            }

            UploadImageRequested?.Invoke(this, new LevantamientoTreeItemActionRequestedEventArgs
            {
                HotspotKey = Item.Clave
            });
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Item is null || string.IsNullOrWhiteSpace(Item.Clave))
            {
                return;
            }

            DeleteRequested?.Invoke(this, new LevantamientoTreeItemActionRequestedEventArgs
            {
                HotspotKey = Item.Clave
            });
        }

    }
}
