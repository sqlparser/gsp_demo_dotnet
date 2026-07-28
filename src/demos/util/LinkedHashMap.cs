using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace gudusoft.gsqlparser.demos.util
{
    public class LinkedHashMap<T, U>
    {
        Dictionary<T, LinkedListNode<Tuple<U, T>>> D = new Dictionary<T, LinkedListNode<Tuple<U, T>>>();
        LinkedList<Tuple<U, T>> LL = new LinkedList<Tuple<U, T>>();

        public U this[T c]
        {
            get
            {
                return D[c].Value.Item1;
            }

            set
            {
                if (D.ContainsKey(c))
                {
                    LL.Remove(D[c]);
                }

                D[c] = new LinkedListNode<Tuple<U, T>>(Tuple.Create(value, c));
                LL.AddLast(D[c]);
            }
        }

        public bool ContainsKey(T k)
        {
            return D.ContainsKey(k);
        }

        /// <summary>
        /// Safe lookup, for the common ported-from-Java pattern of reading a key
        /// that may be absent. Java's Map.get returns null for a missing key,
        /// but the indexer above follows Dictionary and throws
        /// KeyNotFoundException, so a direct port of get() crashes instead of
        /// yielding null. Use this where the key is not known to be present.
        /// </summary>
        public bool TryGetValue(T k, out U value)
        {
            if (D.TryGetValue(k, out LinkedListNode<Tuple<U, T>> node))
            {
                value = node.Value.Item1;
                return true;
            }

            value = default(U);
            return false;
        }

        public bool Remove(T k) {
            if (D.ContainsKey(k))
            {
                LL.Remove(D[k]);
                D.Remove(k);
                return true;
            }
            return false;
        }

        public void Clear() {
            D.Clear();
            LL.Clear();
        }
        public U PopFirst()
        {
            var node = LL.First;
            LL.Remove(node);
            D.Remove(node.Value.Item2);
            return node.Value.Item1;
        }

        public int Count
        {
            get
            {
                return D.Count;
            }
        }

        public Dictionary<T, LinkedListNode<Tuple<U, T>>>.KeyCollection Keys
        {
            get
            {
                return D.Keys;
            }
        }
    }

}
