using System;

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

    using Directory = Lucene.Net.Store.Directory;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using IMutableBits = Lucene.Net.Util.IMutableBits;
    using PerFieldPostingsFormat = Lucene.Net.Codecs.PerField.PerFieldPostingsFormat; // javadocs
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat; // javadocs

    /// <summary>
    /// Holder class for common parameters used during write.
    /// @lucene.experimental
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class SegmentWriteState
    {
        /// <summary>
        /// <seealso cref="InfoStream"/> used for debugging messages. </summary>
        public InfoStream InfoStream { get; private set; }

        /// <summary>
        /// <seealso cref="Directory"/> where this segment will be written
        ///  to.
        /// </summary>
        public Directory Directory { get; private set; }

        /// <summary>
        /// <seealso cref="SegmentInfo"/> describing this segment. </summary>
        public SegmentInfo SegmentInfo { get; private set; }

        /// <summary>
        /// <seealso cref="FieldInfos"/> describing all fields in this
        ///  segment.
        /// </summary>
        public FieldInfos FieldInfos { get; private set; }

        /// <summary>
        /// Number of deleted documents set while flushing the
        ///  segment.
        /// </summary>
        public int DelCountOnFlush { get; set; }

        /// <summary>
        /// Deletes and updates to apply while we are flushing the segment. A Term is
        /// enrolled in here if it was deleted/updated at one point, and it's mapped to
        /// the docIDUpto, meaning any docID &lt; docIDUpto containing this term should
        /// be deleted/updated.
        /// </summary>
        public BufferedUpdates SegUpdates { get; private set; }

        /// <summary>
        /// <seealso cref="IMutableBits"/> recording live documents; this is
        ///  only set if there is one or more deleted documents.
        /// </summary>
        public IMutableBits LiveDocs { get; set; }

        /// <summary>
        /// Unique suffix for any postings files written for this
        ///  segment.  <seealso cref="PerFieldPostingsFormat"/> sets this for
        ///  each of the postings formats it wraps.  If you create
        ///  a new <seealso cref="PostingsFormat"/> then any files you
        ///  write/read must be derived using this suffix (use
        ///  <seealso cref="IndexFileNames#segmentFileName(String,String,String)"/>).
        /// </summary>
        public string SegmentSuffix { get; private set; }

        /// <summary>
        /// Expert: The fraction of terms in the "dictionary" which should be stored
        /// in RAM.  Smaller values use more memory, but make searching slightly
        /// faster, while larger values use less memory and make searching slightly
        /// slower.  Searching is typically not dominated by dictionary lookup, so
        /// tweaking this is rarely useful.
        /// </summary>
        public int TermIndexInterval { get; set; } // TODO: this should be private to the codec, not settable here or in IWC

        /// <summary>
        /// <seealso cref="IOContext"/> for all writes; you should pass this
        ///  to <seealso cref="Directory#createOutput(String,IOContext)"/>.
        /// </summary>
        public IOContext Context { get; private set; }

        /// <summary>
        /// Sole constructor. </summary>
        public SegmentWriteState(InfoStream infoStream, Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, int termIndexInterval, BufferedUpdates segUpdates, IOContext context)
            : this(infoStream, directory, segmentInfo, fieldInfos, termIndexInterval, segUpdates, context, "")
        {
        }

        /// <summary>
        /// Constructor which takes segment suffix.
        /// </summary>
        /// <seealso cref= #SegmentWriteState(InfoStream, Directory, SegmentInfo, FieldInfos, int,
        ///      BufferedUpdates, IOContext) </seealso>
        public SegmentWriteState(InfoStream infoStream, Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, int termIndexInterval, BufferedUpdates segUpdates, IOContext context, string segmentSuffix)
        {
            this.InfoStream = infoStream;
            this.SegUpdates = segUpdates;
            this.Directory = directory;
            this.SegmentInfo = segmentInfo;
            this.FieldInfos = fieldInfos;
            this.TermIndexInterval = termIndexInterval;
            this.SegmentSuffix = segmentSuffix;
            this.Context = context;
        }

        /// <summary>
        /// Create a shallow copy of <seealso cref="SegmentWriteState"/> with a new segment suffix. </summary>
        public SegmentWriteState(SegmentWriteState state, string segmentSuffix)
        {
            InfoStream = state.InfoStream;
            Directory = state.Directory;
            SegmentInfo = state.SegmentInfo;
            FieldInfos = state.FieldInfos;
            TermIndexInterval = state.TermIndexInterval;
            Context = state.Context;
            this.SegmentSuffix = segmentSuffix;
            SegUpdates = state.SegUpdates;
            DelCountOnFlush = state.DelCountOnFlush;
        }
    }
}