using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Mensajeria
{
    public sealed partial class MensajeBurbujaItemView : UserControl
    {
        public event EventHandler<RoutedEventArgs>? PdfButtonClick;
        public event EventHandler<RoutedEventArgs>? OperacionReferenciaClick;
        public event EventHandler<MensajeDto>? ResponderClick;
        public event EventHandler<MensajeDto>? CrearTareaClick;
        public event EventHandler<MensajeDto>? EliminarParaMiClick;
        public event EventHandler<MensajeDto>? EliminarParaTodosClick;
        public event EventHandler<MensajeDto>? RespuestaCitaClick;

        public MensajeBurbujaItemView()
        {
            InitializeComponent();
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e) => PdfButtonClick?.Invoke(sender, e);
        private void OperacionReferenciaButton_Click(object sender, RoutedEventArgs e) => OperacionReferenciaClick?.Invoke(sender, e);

        private MensajeDto? GetMensaje() => DataContext as MensajeDto;

        private void Responder_Click(object sender, RoutedEventArgs e)
        {
            var m = GetMensaje();
            if (m != null) ResponderClick?.Invoke(this, m);
        }

        private void CrearTarea_Click(object sender, RoutedEventArgs e)
        {
            var m = GetMensaje();
            if (m != null) CrearTareaClick?.Invoke(this, m);
        }

        private void EliminarParaMi_Click(object sender, RoutedEventArgs e)
        {
            var m = GetMensaje();
            if (m != null) EliminarParaMiClick?.Invoke(this, m);
        }

        private void EliminarParaTodos_Click(object sender, RoutedEventArgs e)
        {
            var m = GetMensaje();
            if (m != null) EliminarParaTodosClick?.Invoke(this, m);
        }

        private void RespuestaCita_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var m = GetMensaje();
            if (m != null) RespuestaCitaClick?.Invoke(this, m);
            e.Handled = true;
        }
    }
}
