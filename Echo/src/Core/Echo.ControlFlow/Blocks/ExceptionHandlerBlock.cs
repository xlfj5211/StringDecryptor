using System.Collections.Generic;
using System.Linq;

namespace Echo.ControlFlow.Blocks
{
    /// <summary>
    /// Represents a block of region that is protected by a set of exception handler blocks. 
    /// </summary>
    /// <typeparam name="TInstruction">The type of instructions stored in the blocks.</typeparam>
    public class ExceptionHandlerBlock<TInstruction> : IBlock<TInstruction>
    {
        /// <summary>
        /// Gets the protected block.
        /// </summary>
        public ScopeBlock<TInstruction> ProtectedBlock
        {
            get;
        } = new ScopeBlock<TInstruction>();

        /// <summary>
        /// Gets a collection of handler blocks.
        /// </summary>
        public IList<HandlerBlock<TInstruction>> Handlers
        {
            get;
        } = new List<HandlerBlock<TInstruction>>();

        /// <summary>
        /// Gets or sets a user-defined tag that is assigned to this block. 
        /// </summary>
        public object Tag
        {
            get;
            set;
        }
        
        /// <inheritdoc />
        public IEnumerable<BasicBlock<TInstruction>> GetAllBlocks()
        {
            foreach (var block in ProtectedBlock.GetAllBlocks())
                yield return block;

            foreach (var handler in Handlers)
            {
                foreach (var block in handler.GetAllBlocks())
                    yield return block;
            }
        }

        /// <inheritdoc />
        public BasicBlock<TInstruction> GetFirstBlock()
        {
            var result = ProtectedBlock.GetFirstBlock();
            for (int i = 0; i < Handlers.Count && result is null; i++)
                result = Handlers[i].GetFirstBlock();
            return result;
        }

        /// <inheritdoc />
        public BasicBlock<TInstruction> GetLastBlock()
        {
            BasicBlock<TInstruction> result = null;
            for (int i = Handlers.Count - 1; i > 0 && result is null; i--)
                result = Handlers[i].GetFirstBlock();
            return result ?? ProtectedBlock.GetLastBlock();
        }

        /// <inheritdoc />
        public void AcceptVisitor(IBlockVisitor<TInstruction> visitor) => visitor.VisitExceptionHandlerBlock(this);

        /// <inheritdoc />
        public override string ToString() => BlockFormatter<TInstruction>.Format(this);
    }
}