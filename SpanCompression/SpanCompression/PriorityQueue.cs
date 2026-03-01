using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public class PriorityQueue<T> where T : notnull
    {
        private readonly Dictionary<T, int> Index = [];

        private readonly List<T> Heap = [default];

        private readonly IComparer<T> Comparer;

        public int Count => Heap.Count - 1; // How to make a private setter here?

        public PriorityQueue(IComparer<T> comparer)
        {
            Comparer = comparer;
        }

        public void Add(T key)
        {
            int count = Heap.Count;
            Heap.Add(key);
            Index[key] = count;
            FixHeapUp(count);
        }

        public T PopMaximum()
        {
            var max = Heap[1];
            Swap(1, Heap.Count - 1);
            Heap.RemoveAt(Heap.Count - 1);
            Index.Remove(max);
            FixHeapDown(1);
            return max;
        }

        public T PeekMaximum()
        {
            return Heap[1];
        }

        public void Update(T key)
        {
            int index = Index[key];
            FixHeapUp(index);
            FixHeapDown(index);
        }

        private void FixHeapDown(int index)
        {
            int l = index * 2;
            int r = index * 2 + 1;

            if (l >= Heap.Count)
                return;

            if (r == Heap.Count)
                if (Comparer.Compare(Heap[index], Heap[l]) < 0)
                {
                    Swap(index, l);
                    return; // l cannot have children
                }
                else return;

            if (Comparer.Compare(Heap[index], Heap[l]) < 0) // p<l
                if (Comparer.Compare(Heap[l], Heap[r]) < 0) // l<r
                {
                    Swap(index, r);
                    FixHeapDown(r);
                }
                else
                {
                    Swap(index, l);
                    FixHeapDown(l);
                }

            else if (Comparer.Compare(Heap[index], Heap[r]) < 0) // p<r, and p>=l
            {
                Swap(index, r);
                FixHeapDown(r);
            }
        }

        private void FixHeapUp(int index)
        {
            if (index == 1) // root
            {
                //Comparer.Compare(Heap[index], default);
                return;
            }
            else
            {
                if (Comparer.Compare(Heap[index / 2], Heap[index]) < 0)
                {
                    Swap(index, index / 2);
                    FixHeapUp(index / 2);
                }
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = Heap[a];
            Heap[a] = Heap[b];
            Heap[b] = tmp;
            Index[Heap[a]] = a;
            Index[Heap[b]] = b;
        }
    }
}
