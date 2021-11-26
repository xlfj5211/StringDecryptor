using System;
using System.Collections.Generic;
using System.Linq;
using Echo.ControlFlow;
using Echo.ControlFlow.Regions;
using Echo.ControlFlow.Serialization.Blocks;
using Echo.DataFlow;

namespace Echo.Ast.Construction
{
    /// <summary>
    /// Transforms a <see cref="ControlFlowGraph{TInstruction}"/> and a <see cref="DataFlowGraph{TContents}"/> into an Ast
    /// </summary>
    public sealed class AstParser<TInstruction>
    {
        private readonly ControlFlowGraph<TInstruction> _controlFlowGraph;
        private readonly AstArchitecture<TInstruction> _architecture;
        private readonly BlockTransformer<TInstruction> _transformer;
        private readonly Dictionary<ScopeRegion<TInstruction>, ScopeRegion<Statement<TInstruction>>> _regionsMapping =
            new Dictionary<ScopeRegion<TInstruction>, ScopeRegion<Statement<TInstruction>>>();
        
        /// <summary>
        /// Creates a new Ast parser with the given <see cref="ControlFlowGraph{TInstruction}"/>
        /// </summary>
        /// <param name="controlFlowGraph">The <see cref="ControlFlowGraph{TInstruction}"/> to parse</param>
        /// <param name="dataFlowGraph">The <see cref="DataFlowGraph{TContents}"/> to parse</param>
        public AstParser(ControlFlowGraph<TInstruction> controlFlowGraph, DataFlowGraph<TInstruction> dataFlowGraph)
        {
            if (dataFlowGraph == null)
                throw new ArgumentNullException(nameof(dataFlowGraph));
            _controlFlowGraph = controlFlowGraph ?? throw new ArgumentNullException(nameof(controlFlowGraph));
            _architecture = new AstArchitecture<TInstruction>(controlFlowGraph.Architecture);

            var context = new AstParserContext<TInstruction>(_controlFlowGraph, dataFlowGraph);
            _transformer = new BlockTransformer<TInstruction>(context);
        }
        
        /// <summary>
        /// Parses the given <see cref="ControlFlowGraph{TInstruction}"/>
        /// </summary>
        /// <returns>A <see cref="ControlFlowGraph{TInstruction}"/> representing the Ast</returns>
        public ControlFlowGraph<Statement<TInstruction>> Parse()
        {
            var newGraph = new ControlFlowGraph<Statement<TInstruction>>(_architecture);
            var rootScope = _controlFlowGraph.ConstructBlocks();

            // Transform and add regions.
            foreach (var originalRegion in _controlFlowGraph.Regions)
            {
                var newRegion = TransformRegion(originalRegion);
                newGraph.Regions.Add(newRegion);
            }

            // Transform and add nodes.
            foreach (var originalBlock in rootScope.GetAllBlocks())
            {
                var originalNode = _controlFlowGraph.Nodes[originalBlock.Offset];
                var transformedBlock = _transformer.Transform(originalBlock);
                var newNode = new ControlFlowNode<Statement<TInstruction>>(originalBlock.Offset, transformedBlock);
                newGraph.Nodes.Add(newNode);
                
                // Move node to newly created region.
                if (originalNode.ParentRegion is ScopeRegion<TInstruction> basicRegion)
                    newNode.MoveToRegion(_regionsMapping[basicRegion]);
            }

            // Clone edges.
            foreach (var originalEdge in _controlFlowGraph.GetEdges())
            {
                var newOrigin = newGraph.Nodes[originalEdge.Origin.Offset];
                var newTarget = newGraph.Nodes[originalEdge.Target.Offset];
                newOrigin.ConnectWith(newTarget, originalEdge.Type);
            }
            
            // Fix entry point(s).
            newGraph.Entrypoint = newGraph.Nodes[_controlFlowGraph.Entrypoint.Offset];
            FixEntryPoint(_controlFlowGraph);

            return newGraph;

            void FixEntryPoint(IControlFlowRegion<TInstruction> region)
            {
                foreach (var child in region.GetSubRegions())
                    FixEntryPoint(child);

                if (!(region is ScopeRegion<TInstruction> basicControlFlowRegion))
                    return;

                var entry = basicControlFlowRegion.Entrypoint;
                if (entry is null)
                    return;

                _regionsMapping[basicControlFlowRegion].Entrypoint = newGraph.Nodes[entry.Offset];
            }
        }

        private ControlFlowRegion<Statement<TInstruction>> TransformRegion(IControlFlowRegion<TInstruction> region)
        {
            switch (region)
            {
                case ScopeRegion<TInstruction> basicRegion:
                    // Create new basic region.
                    var newBasicRegion = new ScopeRegion<Statement<TInstruction>>();
                    TransformSubRegions(basicRegion, newBasicRegion);

                    // Register basic region pair.
                    _regionsMapping[basicRegion] = newBasicRegion;

                    return newBasicRegion;

                case ExceptionHandlerRegion<TInstruction> ehRegion:
                    var newEhRegion = new ExceptionHandlerRegion<Statement<TInstruction>>();

                    // ProtectedRegion is read-only, so instead we just transform all sub regions and add it to the
                    // existing protected region.
                    TransformSubRegions(ehRegion.ProtectedRegion, newEhRegion.ProtectedRegion);
                    _regionsMapping[ehRegion.ProtectedRegion] = newEhRegion.ProtectedRegion;

                    // Add handler regions.
                    foreach (var subRegion in ehRegion.Handlers)
                        newEhRegion.Handlers.Add(TransformHandlerRegion(subRegion));

                    return newEhRegion;
                
                case HandlerRegion<TInstruction> handlerRegion:
                    return TransformHandlerRegion(handlerRegion);

                default:
                    throw new ArgumentOutOfRangeException(nameof(region));
            }
        }

        private HandlerRegion<Statement<TInstruction>> TransformHandlerRegion(HandlerRegion<TInstruction> handlerRegion)
        {
            var result = new HandlerRegion<Statement<TInstruction>>();

            if (handlerRegion.Prologue != null)
                result.Prologue = (ScopeRegion<Statement<TInstruction>>) TransformRegion(handlerRegion.Prologue);

            if (handlerRegion.Epilogue != null)
                result.Epilogue = (ScopeRegion<Statement<TInstruction>>) TransformRegion(handlerRegion.Epilogue);

            // Contents is read-only, so instead we just transform all sub regions and add it to the
            // existing protected region.
            TransformSubRegions(handlerRegion.Contents, result.Contents);
            _regionsMapping[handlerRegion.Contents] = result.Contents;
            
            return result;
        }

        private void TransformSubRegions(
            ScopeRegion<TInstruction> originalRegion, 
            ScopeRegion<Statement<TInstruction>> newRegion)
        {
            foreach (var subRegion in originalRegion.Regions)
                newRegion.Regions.Add(TransformRegion(subRegion));
        }
    }
}