using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Index
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using AppendingPackedInt64Buffer = Lucene.Net.Util.Packed.AppendingPackedInt64Buffer;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using MonotonicAppendingInt64Buffer = Lucene.Net.Util.Packed.MonotonicAppendingInt64Buffer;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using TermsEnumIndex = Lucene.Net.Index.MultiTermsEnum.TermsEnumIndex;
    using TermsEnumWithSlice = Lucene.Net.Index.MultiTermsEnum.TermsEnumWithSlice;

    /// <summary>
    /// A wrapper for CompositeIndexReader providing access to DocValues.
    ///
    /// <p><b>NOTE</b>: for multi readers, you'll get better
    /// performance by gathering the sub readers using
    /// <seealso cref="IndexReader#getContext()"/> to get the
    /// atomic leaves and then operate per-AtomicReader,
    /// instead of using this class.
    ///
    /// <p><b>NOTE</b>: this is very costly.
    ///
    /// @lucene.experimental
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class MultiDocValues
    {
        /// <summary>
        /// No instantiation </summary>
        private MultiDocValues()
        {
        }

        /// <summary>
        /// Returns a NumericDocValues for a reader's norms (potentially merging on-the-fly).
        /// <p>
        /// this is a slow way to access normalization values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getNormValues(String)"/>
        /// </p>
        /// </summary>
        public static NumericDocValues GetNormValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetNormValues(field);
            }
            FieldInfo fi = MultiFields.GetMergedFieldInfos(r).FieldInfo(field);
            if (fi == null || fi.HasNorms == false)
            {
                return null;
            }

            bool anyReal = false;
            NumericDocValues[] values = new NumericDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                NumericDocValues v = context.AtomicReader.GetNormValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_NUMERIC;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            Debug.Assert(anyReal);

            return new NumericDocValuesAnonymousInnerClassHelper(values, starts);
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private NumericDocValues[] values;
            private int[] starts;

            public NumericDocValuesAnonymousInnerClassHelper(NumericDocValues[] values, int[] starts)
            {
                this.values = values;
                this.starts = starts;
            }

            public override long Get(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                return values[subIndex].Get(docID - starts[subIndex]);
            }
        }

        /// <summary>
        /// Returns a NumericDocValues for a reader's docvalues (potentially merging on-the-fly)
        /// <p>
        /// this is a slow way to access numeric values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getNumericDocValues(String)"/>
        /// </p>
        ///
        /// </summary>
        public static NumericDocValues GetNumericValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetNumericDocValues(field);
            }

            bool anyReal = false;
            NumericDocValues[] values = new NumericDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                NumericDocValues v = context.AtomicReader.GetNumericDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_NUMERIC;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                return new NumericDocValuesAnonymousInnerClassHelper2(values, starts);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private NumericDocValues[] values;
            private int[] starts;

            public NumericDocValuesAnonymousInnerClassHelper2(NumericDocValues[] values, int[] starts)
            {
                this.values = values;
                this.starts = starts;
            }

            public override long Get(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                return values[subIndex].Get(docID - starts[subIndex]);
            }
        }

        /// <summary>
        /// Returns a Bits for a reader's docsWithField (potentially merging on-the-fly)
        /// <p>
        /// this is a slow way to access this bitset. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getDocsWithField(String)"/>
        /// </p>
        ///
        /// </summary>
        public static IBits GetDocsWithField(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;
            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetDocsWithField(field);
            }

            bool anyReal = false;
            bool anyMissing = false;
            IBits[] values = new IBits[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                IBits v = context.AtomicReader.GetDocsWithField(field);
                if (v == null)
                {
                    v = new Lucene.Net.Util.Bits.MatchNoBits(context.Reader.MaxDoc);
                    anyMissing = true;
                }
                else
                {
                    anyReal = true;
                    if (v is Lucene.Net.Util.Bits.MatchAllBits == false)
                    {
                        anyMissing = true;
                    }
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else if (!anyMissing)
            {
                return new Lucene.Net.Util.Bits.MatchAllBits(r.MaxDoc);
            }
            else
            {
                return new MultiBits(values, starts, false);
            }
        }

        /// <summary>
        /// Returns a BinaryDocValues for a reader's docvalues (potentially merging on-the-fly)
        /// <p>
        /// this is a slow way to access binary values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getBinaryDocValues(String)"/>
        /// </p>
        /// </summary>
        public static BinaryDocValues GetBinaryValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetBinaryDocValues(field);
            }

            bool anyReal = false;
            BinaryDocValues[] values = new BinaryDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                BinaryDocValues v = context.AtomicReader.GetBinaryDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_BINARY;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                return new BinaryDocValuesAnonymousInnerClassHelper(values, starts);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private BinaryDocValues[] values;
            private int[] starts;

            public BinaryDocValuesAnonymousInnerClassHelper(BinaryDocValues[] values, int[] starts)
            {
                this.values = values;
                this.starts = starts;
            }

            public override void Get(int docID, BytesRef result)
            {
                int subIndex = ReaderUtil.SubIndex(docID, starts);
                values[subIndex].Get(docID - starts[subIndex], result);
            }
        }

        /// <summary>
        /// Returns a SortedDocValues for a reader's docvalues (potentially doing extremely slow things).
        /// <p>
        /// this is an extremely slow way to access sorted values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getSortedDocValues(String)"/>
        /// </p>
        /// </summary>
        public static SortedDocValues GetSortedValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetSortedDocValues(field);
            }

            bool anyReal = false;
            var values = new SortedDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                SortedDocValues v = context.AtomicReader.GetSortedDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_SORTED;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                TermsEnum[] enums = new TermsEnum[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    enums[i] = values[i].GetTermsEnum();
                }
                OrdinalMap mapping = new OrdinalMap(r.CoreCacheKey, enums);
                return new MultiSortedDocValues(values, starts, mapping);
            }
        }

        /// <summary>
        /// Returns a SortedSetDocValues for a reader's docvalues (potentially doing extremely slow things).
        /// <p>
        /// this is an extremely slow way to access sorted values. Instead, access them per-segment
        /// with <seealso cref="AtomicReader#getSortedSetDocValues(String)"/>
        /// </p>
        /// </summary>
        public static SortedSetDocValues GetSortedSetValues(IndexReader r, string field)
        {
            IList<AtomicReaderContext> leaves = r.Leaves;
            int size = leaves.Count;

            if (size == 0)
            {
                return null;
            }
            else if (size == 1)
            {
                return leaves[0].AtomicReader.GetSortedSetDocValues(field);
            }

            bool anyReal = false;
            SortedSetDocValues[] values = new SortedSetDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = leaves[i];
                SortedSetDocValues v = context.AtomicReader.GetSortedSetDocValues(field);
                if (v == null)
                {
                    v = DocValues.EMPTY_SORTED_SET;
                }
                else
                {
                    anyReal = true;
                }
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = r.MaxDoc;

            if (!anyReal)
            {
                return null;
            }
            else
            {
                TermsEnum[] enums = new TermsEnum[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    enums[i] = values[i].GetTermsEnum();
                }
                OrdinalMap mapping = new OrdinalMap(r.CoreCacheKey, enums);
                return new MultiSortedSetDocValues(values, starts, mapping);
            }
        }

        /// <summary>
        /// maps per-segment ordinals to/from global ordinal space </summary>
        // TODO: use more efficient packed ints structures?
        // TODO: pull this out? its pretty generic (maps between N ord()-enabled TermsEnums)
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class OrdinalMap
        {
            // cache key of whoever asked for this awful thing
            internal readonly object owner;

            // globalOrd -> (globalOrd - segmentOrd) where segmentOrd is the the ordinal in the first segment that contains this term
            internal readonly MonotonicAppendingInt64Buffer globalOrdDeltas;

            // globalOrd -> first segment container
            internal readonly AppendingPackedInt64Buffer firstSegments;

            // for every segment, segmentOrd -> (globalOrd - segmentOrd)
            internal readonly MonotonicAppendingInt64Buffer[] ordDeltas;

            /// <summary>
            /// Creates an ordinal map that allows mapping ords to/from a merged
            /// space from <code>subs</code>. </summary>
            /// <param name="owner"> a cache key </param>
            /// <param name="subs"> TermsEnums that support <seealso cref="TermsEnum#ord()"/>. They need
            ///             not be dense (e.g. can be FilteredTermsEnums}. </param>
            /// <exception cref="IOException"> if an I/O error occurred. </exception>
            public OrdinalMap(object owner, TermsEnum[] subs)
            {
                // create the ordinal mappings by pulling a termsenum over each sub's
                // unique terms, and walking a multitermsenum over those
                this.owner = owner;
                globalOrdDeltas = new MonotonicAppendingInt64Buffer(PackedInt32s.COMPACT);
                firstSegments = new AppendingPackedInt64Buffer(PackedInt32s.COMPACT);
                ordDeltas = new MonotonicAppendingInt64Buffer[subs.Length];
                for (int i = 0; i < ordDeltas.Length; i++)
                {
                    ordDeltas[i] = new MonotonicAppendingInt64Buffer();
                }
                long[] segmentOrds = new long[subs.Length];
                ReaderSlice[] slices = new ReaderSlice[subs.Length];
                TermsEnumIndex[] indexes = new TermsEnumIndex[slices.Length];
                for (int i = 0; i < slices.Length; i++)
                {
                    slices[i] = new ReaderSlice(0, 0, i);
                    indexes[i] = new TermsEnumIndex(subs[i], i);
                }
                MultiTermsEnum mte = new MultiTermsEnum(slices);
                mte.Reset(indexes);
                long globalOrd = 0;
                while (mte.Next() != null)
                {
                    TermsEnumWithSlice[] matches = mte.MatchArray;
                    for (int i = 0; i < mte.MatchCount; i++)
                    {
                        int segmentIndex = matches[i].Index;
                        long segmentOrd = matches[i].Terms.Ord;
                        long delta = globalOrd - segmentOrd;
                        // for each unique term, just mark the first segment index/delta where it occurs
                        if (i == 0)
                        {
                            firstSegments.Add(segmentIndex);
                            globalOrdDeltas.Add(delta);
                        }
                        // for each per-segment ord, map it back to the global term.
                        while (segmentOrds[segmentIndex] <= segmentOrd)
                        {
                            ordDeltas[segmentIndex].Add(delta);
                            segmentOrds[segmentIndex]++;
                        }
                    }
                    globalOrd++;
                }
                firstSegments.Freeze();
                globalOrdDeltas.Freeze();
                for (int i = 0; i < ordDeltas.Length; ++i)
                {
                    ordDeltas[i].Freeze();
                }
            }

            /// <summary>
            /// Given a segment number and segment ordinal, returns
            /// the corresponding global ordinal.
            /// </summary>
            public virtual long GetGlobalOrd(int segmentIndex, long segmentOrd)
            {
                return segmentOrd + ordDeltas[segmentIndex].Get(segmentOrd);
            }

            /// <summary>
            /// Given global ordinal, returns the ordinal of the first segment which contains
            /// this ordinal (the corresponding to the segment return <seealso cref="#getFirstSegmentNumber"/>).
            /// </summary>
            public virtual long GetFirstSegmentOrd(long globalOrd)
            {
                return globalOrd - globalOrdDeltas.Get(globalOrd);
            }

            /// <summary>
            /// Given a global ordinal, returns the index of the first
            /// segment that contains this term.
            /// </summary>
            public virtual int GetFirstSegmentNumber(long globalOrd)
            {
                return (int)firstSegments.Get(globalOrd);
            }

            /// <summary>
            /// Returns the total number of unique terms in global ord space.
            /// </summary>
            public virtual long ValueCount
            {
                get
                {
                    return globalOrdDeltas.Count;
                }
            }

            /// <summary>
            /// Returns total byte size used by this ordinal map.
            /// </summary>
            public virtual long RamBytesUsed()
            {
                long size = globalOrdDeltas.RamBytesUsed() + firstSegments.RamBytesUsed();
                for (int i = 0; i < ordDeltas.Length; i++)
                {
                    size += ordDeltas[i].RamBytesUsed();
                }
                return size;
            }
        }

        /// <summary>
        /// Implements SortedDocValues over n subs, using an OrdinalMap
        /// @lucene.internal
        /// </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class MultiSortedDocValues : SortedDocValues
        {
            /// <summary>
            /// docbase for each leaf: parallel with <seealso cref="#values"/> </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] DocStarts
            {
                get { return docStarts; }
            }
            private readonly int[] docStarts;

            /// <summary>
            /// leaf values </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public SortedDocValues[] Values
            {
                get { return values; }
            }
            private readonly SortedDocValues[] values;

            /// <summary>
            /// ordinal map mapping ords from <code>values</code> to global ord space </summary>
            public OrdinalMap Mapping
            {
                get { return mapping; }
            }
            private readonly OrdinalMap mapping;

            /// <summary>
            /// Creates a new MultiSortedDocValues over <code>values</code> </summary>
            internal MultiSortedDocValues(SortedDocValues[] values, int[] docStarts, OrdinalMap mapping)
            {
                Debug.Assert(values.Length == mapping.ordDeltas.Length);
                Debug.Assert(docStarts.Length == values.Length + 1);
                this.values = values;
                this.docStarts = docStarts;
                this.mapping = mapping;
            }

            public override int GetOrd(int docID)
            {
                int subIndex = ReaderUtil.SubIndex(docID, docStarts);
                int segmentOrd = values[subIndex].GetOrd(docID - docStarts[subIndex]);
                return segmentOrd == -1 ? segmentOrd : (int)mapping.GetGlobalOrd(subIndex, segmentOrd);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                int subIndex = mapping.GetFirstSegmentNumber(ord);
                int segmentOrd = (int)mapping.GetFirstSegmentOrd(ord);
                values[subIndex].LookupOrd(segmentOrd, result);
            }

            public override int ValueCount
            {
                get
                {
                    return (int)mapping.ValueCount;
                }
            }
        }

        /// <summary>
        /// Implements MultiSortedSetDocValues over n subs, using an OrdinalMap
        /// @lucene.internal
        /// </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class MultiSortedSetDocValues : SortedSetDocValues
        {
            /// <summary>
            /// docbase for each leaf: parallel with <seealso cref="#values"/> </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] DocStarts
            {
                get { return docStarts; }
            }
            private readonly int[] docStarts;

            /// <summary>
            /// leaf values </summary>
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public SortedSetDocValues[] Values
            {
                get { return values; }
            }
            private readonly SortedSetDocValues[] values;

            /// <summary>
            /// ordinal map mapping ords from <code>values</code> to global ord space </summary>
            public OrdinalMap Mapping
            {
                get { return mapping; } 
            }
            private readonly OrdinalMap mapping;

            internal int currentSubIndex;

            /// <summary>
            /// Creates a new MultiSortedSetDocValues over <code>values</code> </summary>
            internal MultiSortedSetDocValues(SortedSetDocValues[] values, int[] docStarts, OrdinalMap mapping)
            {
                Debug.Assert(values.Length == mapping.ordDeltas.Length);
                Debug.Assert(docStarts.Length == values.Length + 1);
                this.values = values;
                this.docStarts = docStarts;
                this.mapping = mapping;
            }

            public override long NextOrd()
            {
                long segmentOrd = values[currentSubIndex].NextOrd();
                if (segmentOrd == NO_MORE_ORDS)
                {
                    return segmentOrd;
                }
                else
                {
                    return mapping.GetGlobalOrd(currentSubIndex, segmentOrd);
                }
            }

            public override void SetDocument(int docID)
            {
                currentSubIndex = ReaderUtil.SubIndex(docID, docStarts);
                values[currentSubIndex].SetDocument(docID - docStarts[currentSubIndex]);
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                int subIndex = mapping.GetFirstSegmentNumber(ord);
                long segmentOrd = mapping.GetFirstSegmentOrd(ord);
                values[subIndex].LookupOrd(segmentOrd, result);
            }

            public override long ValueCount
            {
                get
                {
                    return mapping.ValueCount;
                }
            }
        }
    }
}