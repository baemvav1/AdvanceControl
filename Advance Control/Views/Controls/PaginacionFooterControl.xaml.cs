using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels.Common;

namespace Advance_Control.Views.Controls
{
    /// <summary>
    /// Footer de paginación reutilizable para catálogos: controles primero/anterior/
    /// siguiente/último y la leyenda de rango ("1–15 de 89"). Se enlaza a cualquier
    /// instancia de <see cref="PaginadorViewModel{T}"/> vía la propiedad <see cref="Paginador"/>.
    /// </summary>
    public sealed partial class PaginacionFooterControl : UserControl
    {
        public static readonly DependencyProperty PaginadorProperty = DependencyProperty.Register(
            nameof(Paginador),
            typeof(object),
            typeof(PaginacionFooterControl),
            new PropertyMetadata(null, OnPaginadorChanged));

        public object? Paginador
        {
            get => GetValue(PaginadorProperty);
            set => SetValue(PaginadorProperty, value);
        }

        public PaginacionFooterControl()
        {
            this.InitializeComponent();
        }

        private static void OnPaginadorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PaginacionFooterControl)d;

            if (e.OldValue is INotifyPropertyChanged oldNotifier)
                oldNotifier.PropertyChanged -= control.OnPaginadorPropertyChanged;

            if (e.NewValue is INotifyPropertyChanged newNotifier)
                newNotifier.PropertyChanged += control.OnPaginadorPropertyChanged;

            control.Refrescar();
        }

        private void OnPaginadorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
                dispatcherQueue.TryEnqueue(Refrescar);
            else
                Refrescar();
        }

        private void Refrescar()
        {
            if (Paginador is not IPaginador p)
                return;

            RangoTextBlock.Text = p.RangoTexto;
            PaginaTextBlock.Text = p.PaginaTexto;
            PrimeraButton.IsEnabled = p.CanIrAnterior;
            AnteriorButton.IsEnabled = p.CanIrAnterior;
            SiguienteButton.IsEnabled = p.CanIrSiguiente;
            UltimaButton.IsEnabled = p.CanIrSiguiente;
        }

        private void Primera_Click(object sender, RoutedEventArgs e) => (Paginador as IPaginador)?.IrPrimera();
        private void Anterior_Click(object sender, RoutedEventArgs e) => (Paginador as IPaginador)?.IrAnterior();
        private void Siguiente_Click(object sender, RoutedEventArgs e) => (Paginador as IPaginador)?.IrSiguiente();
        private void Ultima_Click(object sender, RoutedEventArgs e) => (Paginador as IPaginador)?.IrUltima();
    }
}
