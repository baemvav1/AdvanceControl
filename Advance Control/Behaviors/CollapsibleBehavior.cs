using System;
using System.Runtime.Versioning;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Advance_Control.Behaviors
{
    /// <summary>
    /// Behavior that makes a target element collapsible by clicking on a trigger element.
    /// Attach this behavior to the trigger element and specify the target element to collapse.
    /// </summary>
    [SupportedOSPlatform("windows10.0.17763.0")]
    public static class CollapsibleBehavior
    {
        // Attached property for the target element to collapse
        public static readonly DependencyProperty TargetElementProperty =
            DependencyProperty.RegisterAttached(
                "TargetElement",
                typeof(FrameworkElement),
                typeof(CollapsibleBehavior),
                new PropertyMetadata(null, OnTargetElementChanged));

        // Attached property to track collapsed state
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.RegisterAttached(
                "IsCollapsed",
                typeof(bool),
                typeof(CollapsibleBehavior),
                new PropertyMetadata(false, OnIsCollapsedChanged));

        // Attached property for animation duration (in milliseconds)
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.RegisterAttached(
                "AnimationDuration",
                typeof(int),
                typeof(CollapsibleBehavior),
                new PropertyMetadata(200));

        // Attached property for the visual indicator element (like an icon that rotates)
        public static readonly DependencyProperty IndicatorElementProperty =
            DependencyProperty.RegisterAttached(
                "IndicatorElement",
                typeof(FrameworkElement),
                typeof(CollapsibleBehavior),
                new PropertyMetadata(null));

        // Getter and Setter for TargetElement
        public static FrameworkElement GetTargetElement(DependencyObject obj)
        {
            return (FrameworkElement)obj.GetValue(TargetElementProperty);
        }

        public static void SetTargetElement(DependencyObject obj, FrameworkElement value)
        {
            obj.SetValue(TargetElementProperty, value);
        }

        // Getter and Setter for IsCollapsed
        public static bool GetIsCollapsed(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsCollapsedProperty);
        }

        public static void SetIsCollapsed(DependencyObject obj, bool value)
        {
            obj.SetValue(IsCollapsedProperty, value);
        }

        // Getter and Setter for AnimationDuration
        public static int GetAnimationDuration(DependencyObject obj)
        {
            return (int)obj.GetValue(AnimationDurationProperty);
        }

        public static void SetAnimationDuration(DependencyObject obj, int value)
        {
            obj.SetValue(AnimationDurationProperty, value);
        }

        // Getter and Setter for IndicatorElement
        public static FrameworkElement GetIndicatorElement(DependencyObject obj)
        {
            return (FrameworkElement)obj.GetValue(IndicatorElementProperty);
        }

        public static void SetIndicatorElement(DependencyObject obj, FrameworkElement value)
        {
            obj.SetValue(IndicatorElementProperty, value);
        }

        private static void OnTargetElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement triggerElement)
            {
                // Remove old handler if it exists
                triggerElement.Tapped -= OnTriggerTapped;
                
                if (e.NewValue is FrameworkElement targetElement)
                {
                    // Add pointer cursor to indicate clickability
                    triggerElement.PointerEntered -= OnPointerEntered;
                    triggerElement.PointerExited -= OnPointerExited;
                    triggerElement.PointerEntered += OnPointerEntered;
                    triggerElement.PointerExited += OnPointerExited;

                    // Attach the tap handler
                    triggerElement.Tapped += OnTriggerTapped;

                    // Initialize the target to collapsed state
                    bool isCollapsed = GetIsCollapsed(triggerElement);
                    ApplyCollapsedState(triggerElement, targetElement, isCollapsed, false);
                }
            }
        }

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement triggerElement)
            {
                var targetElement = GetTargetElement(triggerElement);
                if (targetElement != null)
                {
                    bool isCollapsed = (bool)e.NewValue;
                    ApplyCollapsedState(triggerElement, targetElement, isCollapsed, true);
                }
            }
        }

        private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                // Change cursor to hand to indicate clickability
                element.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            }
        }

        private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                // Reset cursor
                element.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }

        private static void OnTriggerTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement triggerElement)
            {
                // Toggle the collapsed state
                bool currentState = GetIsCollapsed(triggerElement);
                SetIsCollapsed(triggerElement, !currentState);
            }
        }

        private static void ApplyCollapsedState(FrameworkElement triggerElement, FrameworkElement targetElement, bool isCollapsed, bool animate)
        {
            if (targetElement == null) return;

            // Get animation duration from trigger element
            int duration = GetAnimationDuration(triggerElement);

            if (animate)
            {
                // Create animation for smooth collapse/expand
                var storyboard = new Storyboard();
                
                // Height animation
                var heightAnimation = new DoubleAnimation
                {
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                if (isCollapsed)
                {
                    // Store the current height before collapsing
                    if (targetElement.ActualHeight > 0)
                    {
                        targetElement.Tag = targetElement.ActualHeight;
                    }
                    
                    heightAnimation.From = targetElement.ActualHeight;
                    heightAnimation.To = 0;
                }
                else
                {
                    // Restore to original height
                    heightAnimation.From = 0;
                    if (targetElement.Tag is double originalHeight && originalHeight > 0)
                    {
                        heightAnimation.To = originalHeight;
                    }
                    else
                    {
                        // If no stored height, measure the content
                        targetElement.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                        heightAnimation.To = targetElement.DesiredSize.Height;
                    }
                }

                Storyboard.SetTarget(heightAnimation, targetElement);
                Storyboard.SetTargetProperty(heightAnimation, "Height");
                storyboard.Children.Add(heightAnimation);

                // Opacity animation for smoother transition
                var opacityAnimation = new DoubleAnimation
                {
                    Duration = TimeSpan.FromMilliseconds(duration),
                    To = isCollapsed ? 0 : 1,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                Storyboard.SetTarget(opacityAnimation, targetElement);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                storyboard.Children.Add(opacityAnimation);

                // Rotate the indicator element if specified
                var indicatorElement = GetIndicatorElement(triggerElement);
                if (indicatorElement != null)
                {
                    // Ensure the indicator has a RenderTransform
                    if (indicatorElement.RenderTransform == null || !(indicatorElement.RenderTransform is RotateTransform))
                    {
                        indicatorElement.RenderTransform = new RotateTransform();
                        indicatorElement.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                    }

                    var rotateAnimation = new DoubleAnimation
                    {
                        Duration = TimeSpan.FromMilliseconds(duration),
                        To = isCollapsed ? 0 : 180,  // Rotate 180 degrees when expanded
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };

                    Storyboard.SetTarget(rotateAnimation, indicatorElement);
                    Storyboard.SetTargetProperty(rotateAnimation, "(UIElement.RenderTransform).(RotateTransform.Angle)");
                    storyboard.Children.Add(rotateAnimation);
                }

                // Handle completion
                storyboard.Completed += (s, e) =>
                {
                    if (isCollapsed)
                    {
                        targetElement.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        targetElement.Height = double.NaN; // Reset to auto
                    }
                };

                // Make visible before expanding
                if (!isCollapsed)
                {
                    targetElement.Visibility = Visibility.Visible;
                }

                storyboard.Begin();
            }
            else
            {
                // Instant state change without animation
                targetElement.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                targetElement.Opacity = isCollapsed ? 0 : 1;

                // Set indicator rotation instantly
                var indicatorElement = GetIndicatorElement(triggerElement);
                if (indicatorElement != null)
                {
                    if (indicatorElement.RenderTransform == null || !(indicatorElement.RenderTransform is RotateTransform))
                    {
                        indicatorElement.RenderTransform = new RotateTransform();
                        indicatorElement.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                    }
                    ((RotateTransform)indicatorElement.RenderTransform).Angle = isCollapsed ? 0 : 180;
                }
            }
        }
    }
}
