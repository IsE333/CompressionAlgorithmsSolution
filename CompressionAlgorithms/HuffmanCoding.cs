using CompressionAlgorithms.DataStructures;
using System.Collections;

namespace CompressionAlgorithms
{
    public class HuffmanCoding : IAlgorithm
    {
        public string AlgorithmName => "Huffman Coding";

        public byte[] Compress(byte[] data, int dataSize)
        {
            Dictionary<byte, int> frequencyTable = [];
            for (int i = 0; i < dataSize; i++)
                if (frequencyTable.ContainsKey(data[i]))
                    frequencyTable[data[i]] += 1;
                else
                    frequencyTable[data[i]] = 1;

            TrieNode trieRoot = BuildTrie(frequencyTable);
            Dictionary<byte, bool[]> huffmanCodes = trieRoot.GetCodes();
            //PrintCodes(huffmanCodes);
            
            List<bool> encodedHuffmanCodes = trieRoot.EncodeCodes();
            //PrintEncodedCodes(encodedHuffmanCodes);


            List<bool> compressedBits = [];
            for (int i = 0; i < dataSize; i++)
                compressedBits.AddRange(huffmanCodes[data[i]]);

            int size = compressedBits.Count + encodedHuffmanCodes.Count + 1;
            List<bool> paddingBits = [];
            for (int i = 0; i < 8 - size % 8; i++)
                paddingBits.Add(false);
            paddingBits.Add(true);

            //Console.WriteLine("\nFinal Compressed Bits:");
            //for (int i = 0; i < compressedBits.Count; i++)
            //    Console.Write(compressedBits[i] ? "1" : "0");

            //Console.WriteLine();
            //Console.WriteLine($"Huffman Codes Size: {encodedHuffmanCodes.Count} bits");
            //Console.WriteLine($"Padding Size:       {paddingBits.Count} bits");

            byte[] result = new byte[(paddingBits.Count + encodedHuffmanCodes.Count + compressedBits.Count) / 8];
            BitArray bitArray = new(paddingBits.Concat(encodedHuffmanCodes).Concat(compressedBits).ToArray());
            bitArray.CopyTo(result, 0);
            return result;
        }

        public byte[] Decompress(byte[] data)
        {
            List<byte> result = [];
            bool initialPaddingDone = false;
            bool decodingCodesDone = false;

            Dictionary<byte, bool[]> codes = [];

            TrieNode root = new();
            TrieNode currentNode = root;

            BitArray bitArray = new(data);
            for (int i = 0; i < bitArray.Length; i++)
            {
                // Remove initial Padding
                if (!initialPaddingDone)
                {
                    if (bitArray[i] == true)
                        initialPaddingDone = true;
                    continue;
                }

                // Decode Huffman Codes
                if (!decodingCodesDone)
                {
                    if (bitArray[i] == false)
                    {
                        if (!currentNode.Children.ContainsKey(false))
                        {
                            currentNode.Children[false] = new TrieNode(currentNode);
                            currentNode = currentNode.Children[false];
                        } else if (!currentNode.Children.ContainsKey(true))
                        {
                            currentNode.Children[true] = new TrieNode(currentNode);
                            currentNode = currentNode.Children[true];
                        }
                    }
                    else
                    {
                        BitArray byteBits = new(8);
                        for (int j = 0; j < 8; j++)
                            byteBits[j] = bitArray[i + 1 + j];
                        var b = new byte[1];
                        byteBits.CopyTo(b, 0);

                        if (!currentNode.Children.ContainsKey(false))
                        {
                            currentNode.Children[false] = new TrieNode(b[0], currentNode);
                        } else if (!currentNode.Children.ContainsKey(true))
                        {
                            currentNode.Children[true] = new TrieNode(b[0], currentNode);

                            if (currentNode.Prev == null) // child of root is a leaf node
                                decodingCodesDone = true;
                            while (currentNode.Prev != null)
                            {
                                currentNode = currentNode.Prev;
                                if (!currentNode.Children.ContainsKey(true))
                                    break;
                                if (currentNode.Prev == null)
                                    decodingCodesDone = true;
                            }
                        }
                        else
                            throw new Exception("Invalid Huffman Code Structure");
                        i += 8;
                    }
                    continue;
                }

                // Decode Data
                if (bitArray[i])
                    currentNode = currentNode.Children[true];
                else
                    currentNode = currentNode.Children[false];

                if (currentNode.IsLeaf)
                {
                    result.Add(currentNode.Data.Value);
                    currentNode = root;
                }
            }
            return [.. result];
        }

        TrieNode BuildTrie(Dictionary<byte, int> frequencyTable)
        {
            PriorityQueue<TrieNode, int> priorityQueue = new();
            foreach (var kvp in frequencyTable)
            {
                priorityQueue.Enqueue(new TrieNode(kvp.Key), kvp.Value);
            }
            while (priorityQueue.Count > 1)
            {
                priorityQueue.TryDequeue(out var left, out int leftPriority);
                priorityQueue.TryDequeue(out var right, out int rightPriority);

                var mergedNode = new TrieNode(left, right);
                priorityQueue.Enqueue(mergedNode, leftPriority + rightPriority);
            }
            TrieNode root = priorityQueue.Dequeue();
            if (root.IsLeaf)
                root = new TrieNode(root, new TrieNode(0));
            return root;
        }

        void PrintCodes(Dictionary<byte, bool[]> codes)
        {
            Console.WriteLine("Huffman Codes:");
            foreach (var kvp in codes)
                Console.WriteLine($"Byte: {kvp.Key} {(char)kvp.Key}, Code: {string.Join("", kvp.Value.Select(b => b ? "1" : "0"))}");
        }

        void PrintEncodedCodes(List<bool> codeArr)
        {
            Console.WriteLine("\nEncoded Huffman Codes:");
            for (int i = 0; i < codeArr.Count; i++)
            {
                if (codeArr[i] == true)
                {
                    Console.Write($"1 ");
                    for (int j = 1; j <= 8; j++)
                        Console.Write(codeArr[i + j] ? "1" : "0");
                    var a = new BitArray(codeArr[(i + 1)..(i + 9)].ToArray());
                    var b = new byte[1];
                    a.CopyTo(b, 0);
                    Console.Write(" (" + (char)b[0] + ") \n");
                    i += 8;
                }
                else
                {
                    Console.Write("0");
                }
            }
        } 
    }
}
