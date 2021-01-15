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
    /**
* Constants that indicates the scroll state of the Scrollable widgets.
*/
    public enum ScrollState
    {
        /**
     * Widget is stopped.
     * This state does not always mean that this widget have never been scrolled.
     */
        STOP,

    /**
     * Widget is scrolled up by swiping it down.
     */
    UP,

    /**
     * Widget is scrolled down by swiping it up.
     */
    DOWN,
    }
}