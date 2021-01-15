using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TestNestedScrollView
{
    public interface IScrollable
    {
        /**
      * Set a callback listener.<br>
      * Developers should use {@link #addScrollViewCallbacks(ObservableScrollViewCallbacks)}
      * and {@link #removeScrollViewCallbacks(ObservableScrollViewCallbacks)}.
      *
      * @param listener Listener to set.
      */
        void SetScrollViewCallbacks(IObservableScrollViewCallbacks listener);

        /**
         * Add a callback listener.
         *
         * @param listener Listener to add.
         * @since 1.7.0
         */
        void AddScrollViewCallbacks(IObservableScrollViewCallbacks listener);

        /**
         * Remove a callback listener.
         *
         * @param listener Listener to remove.
         * @since 1.7.0
         */
        void RemoveScrollViewCallbacks(IObservableScrollViewCallbacks listener);

        /**
         * Clear callback listeners.
         *
         * @since 1.7.0
         */
        void ClearScrollViewCallbacks();

        /**
         * Scroll vertically to the absolute Y.<br>
         * Implemented classes are expected to scroll to the exact Y pixels from the top,
         * but it depends on the type of the widget.
         *
         * @param y Vertical position to scroll to.
         */
        void ScrollVerticallyTo(int y);

        /**
         * Return the current Y of the scrollable view.
         *
         * @return Current Y pixel.
         */
        int GetCurrentScrollY();

        /**
         * Set a touch motion event delegation ViewGroup.<br>
         * This is used to pass motion events back to parent view.
         * It's up to the implementation classes whether or not it works.
         *
         * @param viewGroup ViewGroup object to dispatch motion events.
         */
        void SetTouchInterceptionViewGroup(ViewGroup viewGroup);
    }
}