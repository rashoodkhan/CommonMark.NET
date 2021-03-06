﻿using CommonMark.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonMark
{
    /// <summary>
    /// An exception that is caught during CommonMark parsing or formatting.
    /// </summary>
#if OptimizeFor45
    [Serializable]
#endif
    public class CommonMarkException : Exception
    {
        /// <summary>
        /// Gets the block that caused the exception. Can be <c>null</c>.
        /// </summary>
        public Block BlockElement { get; private set; }

        /// <summary>
        /// Gets the inline element that caused the exception. Can be <c>null</c>.
        /// </summary>
        public Inline InlineElement { get; private set; }

        public CommonMarkException() { }
        public CommonMarkException(string message) : base(message) { }
        public CommonMarkException(string message, Exception inner) : base(message, inner) { }
        public CommonMarkException(string message, Inline inline, Exception inner = null) : base(message, inner)
        {
            this.InlineElement = inline;
        }
        public CommonMarkException(string message, Block block, Exception inner = null) : base(message, inner) 
        {
            this.BlockElement = block;
        }

#if OptimizeFor45
        protected CommonMarkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) 
        {
            this.BlockElement = (Block)info.GetValue("BlockElement", typeof(Block));
            this.InlineElement = (Inline)info.GetValue("InlineElement", typeof(Inline));
        }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("BlockElement", this.BlockElement);
            info.AddValue("InlineElement", this.InlineElement);
        }
#endif
    }
}
