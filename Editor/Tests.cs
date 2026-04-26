using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Screenplay.Editor
{
    public class Tests
    {
        [UnityTest]
        public IEnumerator TestCancelableAutoResetEventClose()
        {
            var cs = new CancellationSource();
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var care = new CancelableAutoResetEvent<int>();

            TaskCollectCancel(care, tcs1, cs.Token).Forget();
            TaskCollectCancel(care, tcs2, cs.Token).Forget();

            care.Close();

            while (tcs1.Task.IsCompleted == false || tcs2.Task.IsCompleted == false)
            {
                yield return null;
            }

            Assert.True(tcs1.Task.Result);
            Assert.True(tcs2.Task.Result);


            static async UniTask TaskCollectCancel(CancelableAutoResetEvent<int> care, TaskCompletionSource<bool> task, Cancellation cancellation)
            {
                try
                {
                    await care.NextSignal(cancellation);
                }
                catch (OperationCanceledException e)
                {
                    task.TrySetResult(true);
                    return;
                }
                catch (Exception ex)
                {
                    task.TrySetResult(false);
                }

                task.TrySetResult(false);
            }
        }

        [Test]
        public void TestCancelableAutoResetEvent()
        {
            var deque = new Deque<int>(8);
            for (int i = 0; i < 8; ++i)
                deque.AddToBack(i);

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, deque.ToArray());

            for (int i = 0; i < 4; ++i)
                deque.RemoveFromFront();

            Assert.AreEqual(new[] { 4, 5, 6, 7 }, deque.ToArray());

            // Wrapping past end
            for (int i = 0; i < 4; ++i)
                deque.AddToBack(i);

            Assert.AreEqual(new[] { 4, 5, 6, 7, 0, 1, 2, 3 }, deque.ToArray());
            Assert.AreEqual(8, deque.Capacity); // We should be right

            for (int i = 0; i < 2; ++i)
                deque.RemoveFromBack();

            Assert.AreEqual(new[] { 4, 5, 6, 7, 0, 1 }, deque.ToArray());
        }

        [Test]
        public void TestAddAndRemove()
        {
            var deque = new Deque<int>(8);
            for (int i = 0; i < 8; ++i)
                deque.AddToBack(i);

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, deque.ToArray());

            for (int i = 0; i < 4; ++i)
                deque.RemoveFromFront();

            Assert.AreEqual(new[] { 4, 5, 6, 7 }, deque.ToArray());

            // Wrapping past end
            for (int i = 0; i < 4; ++i)
                deque.AddToBack(i);

            Assert.AreEqual(new[] { 4, 5, 6, 7, 0, 1, 2, 3 }, deque.ToArray());
            Assert.AreEqual(8, deque.Capacity); // We should be right

            for (int i = 0; i < 2; ++i)
                deque.RemoveFromBack();

            Assert.AreEqual(new[] { 4, 5, 6, 7, 0, 1 }, deque.ToArray());
        }

        [Test]
        public void TestRangeInsertion()
        {
            var deque = new Deque<int>(8);
            deque.AddToFront(1010);
            deque.InsertRange(0, new[]{0, 1, 2, 3, 4, 5, 6});

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 1010 }, deque.ToArray());
            Assert.AreEqual(8, deque.Capacity);

            // Inserting past the end of the buffer
            deque.RemoveRange(0, 7);
            deque.InsertRange(1, new[]{0, 1, 2, 3});

            Assert.AreEqual(new[] { 1010, 0, 1, 2, 3 }, deque.ToArray());
            Assert.AreEqual(8, deque.Capacity);

            deque.InsertRange(0, new[]{0, 1, 2, 3});

            Assert.AreEqual(new[] { 0, 1, 2, 3, 1010, 0, 1, 2, 3 }, deque.ToArray());
            Assert.AreEqual(16, deque.Capacity);
        }

        [Test]
        public void TestBinarySearch()
        {
            var deque = new Deque<int>(8);
            deque.InsertRange(0, new[]{0, 1, 2, 3, 4, 5, 6, 7});

            Assert.AreEqual(5, deque.BinarySearch(5));

            // Inserting past the end of the buffer to test binary search on a split buffer
            deque.RemoveRange(0, 4);
            deque.InsertRange(4, new[]{8, 9, 10, 11});

            Assert.AreEqual(8, deque.Capacity);
            Assert.AreEqual(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

            Assert.AreEqual(5, deque.BinarySearch(9));

            // Test for sorted insertion
            deque.RemoveAt(5);
            deque.Insert(~deque.BinarySearch(9), 9);

            Assert.AreEqual(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

            // ... at buffer end
            deque.RemoveAt(3);
            deque.Insert(~deque.BinarySearch(7), 7);

            Assert.AreEqual(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

            // ... at buffer start
            deque.RemoveAt(4);
            deque.Insert(~deque.BinarySearch(8), 8);

            Assert.AreEqual(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

            // ... on the last element
            deque.RemoveAt(7);
            deque.Insert(~deque.BinarySearch(11), 11);

            Assert.AreEqual(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

            // ... on the first element
            deque.RemoveAt(0);
            deque.Insert(~deque.BinarySearch(3), 3);

            Assert.AreEqual(new[] { 3, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());
        }
    }
}
