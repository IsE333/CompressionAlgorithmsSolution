using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressionAlgorithms.DataStructures
{
    public class TrieNode
    {
        public Dictionary<bool, TrieNode> Children { get; set; }
        public TrieNode? Prev { get; set; }
        public byte? Data { get; set; }
        public bool IsLeaf { get; set; }
        public TrieNode(TrieNode? prev = null)
        {
            Children = [];
            IsLeaf = false;
            Prev = prev;
        }
        public TrieNode(byte? data, TrieNode? prev = null) : this()
        {
            Data = data;
            IsLeaf = data.HasValue;
            Prev = prev;
        }

        public TrieNode(TrieNode left, TrieNode right) : this()
        {
            Children[false] = left;
            Children[true] = right;
            IsLeaf = false;
            left.Prev = this;
            right.Prev = this;
        }

        public Dictionary<byte, bool[]> GetCodes(bool? isLeft = null)
        {
            Dictionary<byte, bool[]> result = [];
            if (IsLeaf && Data.HasValue && isLeft.HasValue) // isLeft is null for root
            {
                result[Data.Value] = [isLeft.Value];
                return result;
            }

            foreach (var child in Children)
            {
                var paths = child.Value.GetCodes(child.Key);
                foreach (var path in paths)
                    result.Add(path.Key, isLeft.HasValue ? [isLeft.Value, ..path.Value] : path.Value);
            }
            return result;
        }

        public List<bool> EncodeCodes(bool isRoot = true)
        {
            List<bool> result = [];

            if (IsLeaf && Data.HasValue)
            {
                var bitArray = new BitArray(bytes:[Data.Value]);
                result.Add(true); // node with value
                foreach (bool bit in bitArray)
                    result.Add(bit);
                return result;
            }

            if (!isRoot)
                result.Add(false); // node without value
            foreach (var child in Children)
                result.AddRange(child.Value.EncodeCodes(false));

            return result;
        }
    }
}
