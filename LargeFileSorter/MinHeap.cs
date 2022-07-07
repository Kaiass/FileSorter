using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeFileSorter
{
    /// <summary>
    /// MinHeap node containing index and value of element
    /// </summary>
    internal class Node
    {
        public int Index;
        public string Value;
    }

    /// <summary>
    /// MinHeap data structure for quick search of minimum element in list
    /// </summary>
    internal class MinHeap
    {
        IComparer<string> _sc;
        IList<Node> _list;
        int _head = 1;

        public MinHeap(IComparer<string> sc)
        {
            // Use 1-based indexing to make list more human readable
            _list = new List<Node> { default(Node) };
            _sc = sc ?? Comparer<string>.Default;
        }
        private int getParent(int i)
        {
            return i / 2;
        }
        private int getLeftChild(int i)
        {
            return i * 2;
        }
        private int getRightChild(int i)
        {
            return i * 2 + 1;
        }
        public int Count
        {
            get
            {
                return _list.Count - 1;
            }
        }
        public void Insert(Node node)
        {
            _list.Add(node);
            var cur = Count;
            // Put the node up to its correct place
            var par = getParent(cur);
            while (cur != _head && _sc.Compare(_list[cur].Value, _list[par].Value) < 0)
            {
                var temp = _list[par];
                _list[par] = _list[cur];
                _list[cur] = temp;
                cur = getParent(cur);
                par = getParent(cur);
            }
        }
        public Node Peek()
        {
            if (_list.Count == _head) return default(Node);

            return _list[_head];
        }
        public Node Extract()
        {
            var peek = Peek();
            if (peek == null) return peek;

            _list[_head] = _list[_list.Count - 1];
            _list.RemoveAt(_list.Count - 1);
            var cur = _head;
            // Put the node down, swap with smallest child
            while (cur != _list.Count - 1)
            {
                var swap = cur;
                var l = getLeftChild(cur);
                if (l < _list.Count && _sc.Compare(_list[l].Value, _list[swap].Value) < 0)
                {
                    swap = l;
                }
                var r = getRightChild(cur);
                if (r < _list.Count && _sc.Compare(_list[r].Value, _list[swap].Value) < 0)
                {
                    swap = r;
                }
                if (swap == cur)
                {
                    break;
                }
                var temp = _list[cur];
                _list[cur] = _list[swap];
                _list[swap] = temp;
                cur = swap;
            }

            return peek;
        }

        /// <summary>
        /// Construct MinHeap using list of nodes
        /// </summary>
        /// <param name="list">List of nodes</param>
        public void Heapify(IList<Node> list)
        {
            // Const
            foreach (var node in list)
            {
                if (node != null) Insert(node);
            }
        }

        /// <summary>
        /// Construct MinHeap using first elements of lists
        /// </summary>
        /// <param name="lists">List array</param>
        public void Heapify(List<string>[] lists)
        {
            int i = 0;
            foreach (var node in lists)
            {
                if (node != null) Insert(new Node() { Index = i, Value = node[0] });
                i++;
            }
        }
    }
}
