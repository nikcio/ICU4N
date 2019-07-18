﻿using ICU4N.Impl;

namespace ICU4N.Text
{
    /// <summary>
    /// Implementation of <see cref="UCaseProps.IContextIterator"/>, iterates over a <see cref="IReplaceable"/>.
    /// See casetrn.cpp/utrans_rep_caseContextIterator().
    /// </summary>
    internal class ReplaceableContextIterator : UCaseProps.IContextIterator
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal ReplaceableContextIterator()
        {
            this.rep = null;
            limit = cpStart = cpLimit = index = contextStart = contextLimit = 0;
            dir = 0;
            reachedLimit = false;
        }

        /// <summary>
        /// Set the text for iteration.
        /// </summary>
        /// <param name="rep"><see cref="IReplaceable"/> to iterate over.</param>
        public virtual void SetText(IReplaceable rep)
        {
            this.rep = rep;
            limit = contextLimit = rep.Length;
            cpStart = cpLimit = index = contextStart = 0;
            dir = 0;
            reachedLimit = false;
        }

        /// <summary>
        /// Set the index where <see cref="NextCaseMapCP()"/> is to start iterating.
        /// </summary>
        /// <param name="index">Iteration start index for <see cref="NextCaseMapCP()"/>.</param>
        public virtual void SetIndex(int index)
        {
            cpStart = cpLimit = index;
            this.index = 0;
            dir = 0;
            reachedLimit = false;
        }

        /// <summary>
        /// Get the index of where the code point currently being case-mapped starts.
        /// </summary>
        /// <returns>The start index of the current code point.</returns>
        public virtual int CaseMapCPStart
        {
            get { return cpStart; }
        }

        /// <summary>
        /// Set the iteration limit for <see cref="NextCaseMapCP()"/> to an index within the string.
        /// If the limit parameter is negative or past the string, then the
        /// string length is restored as the iteration limit.
        /// </summary>
        /// <param name="lim">The iteration limit.</param>
        public virtual void SetLimit(int lim)
        {
            if (0 <= lim && lim <= rep.Length)
            {
                limit = lim;
            }
            else
            {
                limit = rep.Length;
            }
            reachedLimit = false;
        }

        /// <summary>
        /// Set the start and limit indexes for context iteration with <see cref="Next()"/>.
        /// </summary>
        /// <param name="contextStart">Start of context for <see cref="Next()"/>.</param>
        /// <param name="contextLimit">Limit of context for <see cref="Next()"/>.</param>
        public virtual void SetContextLimits(int contextStart, int contextLimit)
        {
            if (contextStart < 0)
            {
                this.contextStart = 0;
            }
            else if (contextStart <= rep.Length)
            {
                this.contextStart = contextStart;
            }
            else
            {
                this.contextStart = rep.Length;
            }
            if (contextLimit < this.contextStart)
            {
                this.contextLimit = this.contextStart;
            }
            else if (contextLimit <= rep.Length)
            {
                this.contextLimit = contextLimit;
            }
            else
            {
                this.contextLimit = rep.Length;
            }
            reachedLimit = false;
        }

        /// <summary>
        /// Iterate forward through the string to fetch the next code point
        /// to be case-mapped, and set the context indexes for it.
        /// </summary>
        /// <returns>The next code point to be case-mapped, or &lt;0 when the iteration is done.</returns>
        public virtual int NextCaseMapCP()
        {
            int c;
            if (cpLimit < limit)
            {
                cpStart = cpLimit;
                c = rep.Char32At(cpLimit);
                cpLimit += UTF16.GetCharCount(c);
                return c;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Replace the current code point by its case mapping,
        /// and update the indexes.
        /// </summary>
        /// <param name="text">Replacement text.</param>
        /// <returns>The delta for the change of the text length.</returns>
        public virtual int Replace(string text)
        {
            int delta = text.Length - (cpLimit - cpStart);
            rep.Replace(cpStart, cpLimit, text);
            cpLimit += delta;
            limit += delta;
            contextLimit += delta;
            return delta;
        }

        /// <summary>
        /// Did forward context iteration with <see cref="Next()"/> reach the iteration limit?
        /// </summary>
        public virtual bool DidReachLimit
        {
            get { return reachedLimit; }
        }

        // implement UCaseProps.ContextIterator
        public virtual void Reset(int direction)
        {
            if (direction > 0)
            {
                /* reset for forward iteration */
                this.dir = 1;
                index = cpLimit;
            }
            else if (direction < 0)
            {
                /* reset for backward iteration */
                this.dir = -1;
                index = cpStart;
            }
            else
            {
                // not a valid direction
                this.dir = 0;
                index = 0;
            }
            reachedLimit = false;
        }

        public virtual int Next()
        {
            int c;

            if (dir > 0)
            {
                if (index < contextLimit)
                {
                    c = rep.Char32At(index);
                    index += UTF16.GetCharCount(c);
                    return c;
                }
                else
                {
                    // forward context iteration reached the limit
                    reachedLimit = true;
                }
            }
            else if (dir < 0 && index > contextStart)
            {
                c = rep.Char32At(index - 1);
                index -= UTF16.GetCharCount(c);
                return c;
            }
            return -1;
        }

        // variables
        protected IReplaceable rep;
        protected int index, limit, cpStart, cpLimit, contextStart, contextLimit;
        protected int dir; // 0=initial state  >0=forward  <0=backward
        protected bool reachedLimit;
    }
}
