﻿using System.Collections.Generic;
using Echo.Core.Code;

namespace Echo.Ast.Analysis
{
    internal sealed class ReadVariableFinderWalker<TInstruction> : AstNodeWalkerBase<TInstruction>
    {
        internal int Count => Variables.Count;

        internal HashSet<IVariable> Variables
        {
            get;
        } = new HashSet<IVariable>();

        protected override void VisitVariableExpression(VariableExpression<TInstruction> variableExpression) =>
            Variables.Add(variableExpression.Variable);
    }
}