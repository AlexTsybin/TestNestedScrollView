using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Interop;
using Java.Lang;
using TestNestedScrollView;

namespace TestNestedScrollView
{
    public class ObservableNestedScrollView : NestedScrollView, IScrollable
    {
        // Fields that should be saved onSaveInstanceState
        private int mPrevScrollY;
        private int mScrollY;

        // Fields that don't need to be saved onSaveInstanceState
        private IObservableScrollViewCallbacks mCallbacks;
        private List<IObservableScrollViewCallbacks> mCallbackCollection;
        private TestNestedScrollView.ScrollState mScrollState;
        private bool mFirstScroll;
        private bool mDragging;
        private bool mIntercepted;
        private MotionEvent mPrevMoveEvent;
        private ViewGroup mTouchInterceptionViewGroup;

        private const int MAX_SCROLL_FACTOR = 1;
        private bool isAutoScrolling;

        public ObservableNestedScrollView(Context context) : base(context)
        {
        }

        public ObservableNestedScrollView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ObservableNestedScrollView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        protected ObservableNestedScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            SavedState ss = (SavedState)state;
            mPrevScrollY = ss.prevScrollY;
            mScrollY = ss.scrollY;
            base.OnRestoreInstanceState(ss.GetSuperState());
        }

        protected override IParcelable OnSaveInstanceState()
        {
            IParcelable superState = base.OnSaveInstanceState();
            SavedState ss = new SavedState(superState);
            ss.prevScrollY = mPrevScrollY;
            ss.scrollY = mScrollY;
            return ss;
        }

        protected override void OnScrollChanged(int x, int y, int oldX, int oldY)
        {
            base.OnScrollChanged(x, y, oldX, oldY);

            if (isAutoScrolling)
            {
                if (System.Math.Abs(y - oldY) < MAX_SCROLL_FACTOR || y >= MeasuredHeight || y == 0
                        || System.Math.Abs(x - oldX) < MAX_SCROLL_FACTOR || x >= MeasuredWidth || x == 0)
                {
                    isAutoScrolling = false;
                }
            }

            if (hasNoCallbacks())
            {
                return;
            }
            mScrollY = y;

            DispatchOnScrollChanged(y, mFirstScroll, mDragging);
            if (mFirstScroll)
            {
                mFirstScroll = false;
            }

            if (mPrevScrollY < y)
            {
                mScrollState = TestNestedScrollView.ScrollState.UP;
            }
            else if (y < mPrevScrollY)
            {
                mScrollState = TestNestedScrollView.ScrollState.DOWN;
                //} else {
                // Keep previous state while dragging.
                // Never makes it STOP even if scrollY not changed.
                // Before Android 4.4, onTouchEvent calls onScrollChanged directly for ACTION_MOVE,
                // which makes mScrollState always STOP when onUpOrCancelMotionEvent is called.
                // STOP state is now meaningless for ScrollView.
            }
            mPrevScrollY = y;
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (isAutoScrolling)
            {
                return base.OnTouchEvent(ev);
            }

            if (hasNoCallbacks())
            {
                return base.OnInterceptTouchEvent(ev);
            }
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    mDragging = false;
                    DispatchOnUpOrCancelMotionEvent(mScrollState);
                    return false;
                case MotionEventActions.Down:
                    // Whether or not motion events are consumed by children,
                    // flag initializations which are related to ACTION_DOWN events should be executed.
                    // Because if the ACTION_DOWN is consumed by children and only ACTION_MOVEs are
                    // passed to parent (this view), the flags will be invalid.
                    // Also, applications might implement initialization codes to onDownMotionEvent,
                    // so call it here.
                    mFirstScroll = mDragging = true;
                    DispatchOnDownMotionEvent();
                    break;
            }
            return base.OnInterceptTouchEvent(ev);
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (isAutoScrolling)
            {
                return base.OnTouchEvent(ev);
            }

            if (hasNoCallbacks())
            {
                return base.OnTouchEvent(ev);
            }

            switch (ev.ActionMasked)
            {
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    mIntercepted = false;
                    mDragging = false;
                    DispatchOnUpOrCancelMotionEvent(mScrollState);
                    break;
                case MotionEventActions.Move:
                    if (mPrevMoveEvent == null)
                    {
                        mPrevMoveEvent = ev;
                    }
                    float diffY = ev.GetY() - mPrevMoveEvent.GetY();
                    mPrevMoveEvent = MotionEvent.ObtainNoHistory(ev);
                    if (GetCurrentScrollY() - diffY <= 0)
                    {
                        // Can't scroll anymore.

                        if (mIntercepted)
                        {
                            // Already dispatched ACTION_DOWN event to parents, so stop here.
                            return false;
                        }

                        // Apps can set the interception target other than the direct parent.
                        ViewGroup parent;
                        if (mTouchInterceptionViewGroup == null)
                        {
                            parent = (ViewGroup)Parent;
                        }
                        else
                        {
                            parent = mTouchInterceptionViewGroup;
                        }

                        // Get offset to parents. If the parent is not the direct parent,
                        // we should aggregate offsets from all of the parents.
                        float offsetX = 0;
                        float offsetY = 0;
                        for (View v = this; v != null && v != parent; v = (View)v.Parent)
                        {
                            offsetX += v.Left - v.ScrollX;
                            offsetY += v.Top - v.ScrollY;
                        }

                        MotionEvent mEvent = MotionEvent.ObtainNoHistory(ev);
                        mEvent.OffsetLocation(offsetX, offsetY);

                        if (parent.OnInterceptTouchEvent(mEvent))
                        {
                            mIntercepted = true;

                            // If the parent wants to intercept ACTION_MOVE events,
                            // we pass ACTION_DOWN event to the parent
                            // as if these touch events just have began now.
                            mEvent.Action = MotionEventActions.Down;

                            // Return this onTouchEvent() first and set ACTION_DOWN event for parent
                            // to the queue, to keep events sequence.
                            Post(new RunnableAnonymousInnerClassHelper(parent, mEvent));
                            return false;
                        }
                        // Even when this can't be scrolled anymore,
                        // simply returning false here may cause subView's click,
                        // so delegate it to super.
                        return base.OnTouchEvent(ev);
                    }
                    break;
            }

            return base.OnTouchEvent(ev);
        }

        public void SetScrollViewCallbacks(IObservableScrollViewCallbacks listener)
        {
            mCallbacks = listener;
        }

        public void AddScrollViewCallbacks(IObservableScrollViewCallbacks listener)
        {
            if (mCallbackCollection == null)
            {
                mCallbackCollection = new List<IObservableScrollViewCallbacks>();
            }
            mCallbackCollection.Add(listener);
        }

        public void RemoveScrollViewCallbacks(IObservableScrollViewCallbacks listener)
        {
            if (mCallbackCollection != null)
            {
                mCallbackCollection.Remove(listener);
            }
        }

        public void ClearScrollViewCallbacks()
        {
            if (mCallbackCollection != null)
            {
                mCallbackCollection.Clear();
            }
        }

        public int GetCurrentScrollY()
        {
            return mScrollY;
        }

        public void ScrollVerticallyTo(int y)
        {
            ScrollTo(0, y);
        }

        public override void ScrollTo(int x, int y)
        {
            isAutoScrolling = true;
            base.ScrollTo(x, y);
        }

        public void SetTouchInterceptionViewGroup(ViewGroup viewGroup)
        {
            mTouchInterceptionViewGroup = viewGroup;
        }

        private bool hasNoCallbacks()
        {
            return mCallbacks == null && mCallbackCollection == null;
        }

        private void DispatchOnDownMotionEvent()
        {
            if (mCallbacks != null)
            {
                mCallbacks.OnDownMotionEvent();
            }
            if (mCallbackCollection != null)
            {
                for (int i = 0; i < mCallbackCollection.Count; i++)
                {
                    IObservableScrollViewCallbacks callbacks = mCallbackCollection[i];
                    callbacks.OnDownMotionEvent();
                }
            }
        }

        private void DispatchOnScrollChanged(int scrollY, bool firstScroll, bool dragging)
        {
            if (mCallbacks != null)
            {
                mCallbacks.OnScrollChanged(scrollY, firstScroll, dragging);
            }
            if (mCallbackCollection != null)
            {
                for (int i = 0; i < mCallbackCollection.Count; i++)
                {
                    IObservableScrollViewCallbacks callbacks = mCallbackCollection[i];
                    callbacks.OnScrollChanged(scrollY, firstScroll, dragging);
                }
            }
        }

        private void DispatchOnUpOrCancelMotionEvent(TestNestedScrollView.ScrollState scrollState)
        {
            if (mCallbacks != null)
            {
                mCallbacks.OnUpOrCancelMotionEvent(scrollState);
            }
            if (mCallbackCollection != null)
            {
                for (int i = 0; i < mCallbackCollection.Count; i++)
                {
                    IObservableScrollViewCallbacks callbacks = mCallbackCollection[i];
                    callbacks.OnUpOrCancelMotionEvent(scrollState);
                }
            }
        }

        private class RunnableAnonymousInnerClassHelper : Java.Lang.Object, IRunnable
        {
            private ViewGroup _parent;
            private MotionEvent _mEvent;

            public RunnableAnonymousInnerClassHelper(ViewGroup parent, MotionEvent mEvent)
            {
                this._parent = parent;
                this._mEvent = mEvent;
            }

            public void Run()
            {
                _parent.DispatchTouchEvent(_mEvent);
            }
        }

        private class SavedState : BaseSavedState
        {
            public int prevScrollY;
            public int scrollY;

            // This keeps the parent(RecyclerView)'s state
            IParcelable superState;

            /**
             * Called by onSaveInstanceState.
             */
            public SavedState(IParcelable superState) : base(superState)
            {
                this.superState = superState != null ? superState : null;
            }

            /**
             * Called by CREATOR.
             */
            private SavedState(Parcel prc) : base(prc)
            {
                prevScrollY = prc.ReadInt();
                scrollY = prc.ReadInt();
            }

            public override void WriteToParcel(Parcel dest, [GeneratedEnum] ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);
                dest.WriteInt(prevScrollY);
                dest.WriteInt(scrollY);
            }

            public IParcelable GetSuperState()
            {
                return superState;
            }

            private static readonly GenericParcelableCreatorForNestedScroll<SavedState> CREATOR
                = new GenericParcelableCreatorForNestedScroll<SavedState>((parcel) => new SavedState(parcel));

            [ExportField("CREATOR")]
            public static GenericParcelableCreatorForNestedScroll<SavedState> GetCreator()
            {
                return CREATOR;
            }
        }
    }
}