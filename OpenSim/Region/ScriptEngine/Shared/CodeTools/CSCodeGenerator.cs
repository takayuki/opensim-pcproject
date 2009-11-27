/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using System.Collections.Generic;
using Tools;

namespace OpenSim.Region.ScriptEngine.Shared.CodeTools
{
    public class CSCodeGenerator : ICodeConverter
    {
        private SYMBOL m_astRoot = null;
        private Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> m_positionMap;
        private int m_indentWidth = 4;  // for indentation
        private int m_braceCount;       // for indentation
        private int m_CSharpLine;       // the current line of generated C# code
        private int m_CSharpCol;        // the current column of generated C# code
        private List<string> m_warnings = new List<string>();

        /// <summary>
        /// Creates an 'empty' CSCodeGenerator instance.
        /// </summary>
        public CSCodeGenerator()
        {
            ResetCounters();
        }

        /// <summary>
        /// Get the mapping between LSL and C# line/column number.
        /// </summary>
        /// <returns>Dictionary\<KeyValuePair\<int, int\>, KeyValuePair\<int, int\>\>.</returns>
        public Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap
        {
            get { return m_positionMap; }
        }

        /// <summary>
        /// Get the mapping between LSL and C# line/column number.
        /// </summary>
        /// <returns>SYMBOL pointing to root of the abstract syntax tree.</returns>
        public SYMBOL ASTRoot
        {
            get { return m_astRoot; }
        }

        /// <summary>
        /// Resets various counters and metadata.
        /// </summary>
        private void ResetCounters()
        {
            m_braceCount = 0;
            m_CSharpLine = 0;
            m_CSharpCol = 1;
            m_positionMap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();
            m_astRoot = null;
        }

        /// <summary>
        /// Generate the code from the AST we have.
        /// </summary>
        /// <param name="script">The LSL source as a string.</param>
        /// <returns>String containing the generated C# code.</returns>
        public string Convert(string script)
        {
            m_warnings.Clear();
            ResetCounters();
            Parser p = new LSLSyntax(new yyLSLSyntax(), new ErrorHandler(true));

            LSL2CSCodeTransformer codeTransformer;
            try
            {
                codeTransformer = new LSL2CSCodeTransformer(p.Parse(script));
            }
            catch (CSToolsException e)
            {
                string message;

                // LL start numbering lines at 0 - geeks!
                // Also need to subtract one line we prepend!
                //
                string emessage = e.Message;
                string slinfo = e.slInfo.ToString();

                // Remove wrong line number info
                //
                if (emessage.StartsWith(slinfo+": "))
                    emessage = emessage.Substring(slinfo.Length+2);

                message = String.Format("Line ({0},{1}) {2}",
                        e.slInfo.lineNumber - 2,
                        e.slInfo.charPosition - 1, emessage);

                throw new Exception(message);
            }

            m_astRoot = codeTransformer.Transform();

            string retstr = String.Empty;

            // standard preamble
            //retstr = GenerateLine("using OpenSim.Region.ScriptEngine.Common;");
            //retstr += GenerateLine("using System.Collections.Generic;");
            //retstr += GenerateLine("");
            //retstr += GenerateLine("namespace SecondLife");
            //retstr += GenerateLine("{");
            m_braceCount++;
            //retstr += GenerateIndentedLine("public class Script : OpenSim.Region.ScriptEngine.Common");
            //retstr += GenerateIndentedLine("{");
            m_braceCount++;

            // line number
            m_CSharpLine += 3;

            // here's the payload
            retstr += GenerateLine();
            foreach (SYMBOL s in m_astRoot.kids)
                retstr += GenerateNode(s);

            // close braces!
            m_braceCount--;
            //retstr += GenerateIndentedLine("}");
            m_braceCount--;
            //retstr += GenerateLine("}");

            // Removes all carriage return characters which may be generated in Windows platform. Is there
            // cleaner way of doing this?
            retstr=retstr.Replace("\r", "");

            return retstr;
        }

        /// <summary>
        /// Get the set of warnings generated during compilation.
        /// </summary>
        /// <returns></returns>
        public string[] GetWarnings()
        {
            return m_warnings.ToArray();
        }

        private void AddWarning(string warning)
        {
            if (!m_warnings.Contains(warning))
            {
                m_warnings.Add(warning);
            }
        }

        /// <summary>
        /// Recursively called to generate each type of node. Will generate this
        /// node, then all it's children.
        /// </summary>
        /// <param name="s">The current node to generate code for.</param>
        /// <returns>String containing C# code for SYMBOL s.</returns>
        private string GenerateNode(SYMBOL s)
        {
            string retstr = String.Empty;

            // make sure to put type lower in the inheritance hierarchy first
            // ie: since IdentArgument and ExpressionArgument inherit from
            // Argument, put IdentArgument and ExpressionArgument before Argument
            if (s is GlobalFunctionDefinition)
                retstr += GenerateGlobalFunctionDefinition((GlobalFunctionDefinition) s);
            else if (s is GlobalVariableDeclaration)
                retstr += GenerateGlobalVariableDeclaration((GlobalVariableDeclaration) s);
            else if (s is State)
                retstr += GenerateState((State) s);
            else if (s is CompoundStatement)
                retstr += GenerateCompoundStatement((CompoundStatement) s);
            else if (s is Declaration)
                retstr += GenerateDeclaration((Declaration) s);
            else if (s is Statement)
                retstr += GenerateStatement((Statement) s);
            else if (s is ReturnStatement)
                retstr += GenerateReturnStatement((ReturnStatement) s);
            else if (s is JumpLabel)
                retstr += GenerateJumpLabel((JumpLabel) s);
            else if (s is JumpStatement)
                retstr += GenerateJumpStatement((JumpStatement) s);
            else if (s is StateChange)
                retstr += GenerateStateChange((StateChange) s);
            else if (s is IfStatement)
                retstr += GenerateIfStatement((IfStatement) s);
            else if (s is WhileStatement)
                retstr += GenerateWhileStatement((WhileStatement) s);
            else if (s is DoWhileStatement)
                retstr += GenerateDoWhileStatement((DoWhileStatement) s);
            else if (s is ForLoop)
                retstr += GenerateForLoop((ForLoop) s);
            else if (s is ArgumentList)
                retstr += GenerateArgumentList((ArgumentList) s);
            else if (s is Assignment)
                retstr += GenerateAssignment((Assignment) s);
            else if (s is BinaryExpression)
                retstr += GenerateBinaryExpression((BinaryExpression) s);
            else if (s is ParenthesisExpression)
                retstr += GenerateParenthesisExpression((ParenthesisExpression) s);
            else if (s is UnaryExpression)
                retstr += GenerateUnaryExpression((UnaryExpression) s);
            else if (s is IncrementDecrementExpression)
                retstr += GenerateIncrementDecrementExpression((IncrementDecrementExpression) s);
            else if (s is TypecastExpression)
                retstr += GenerateTypecastExpression((TypecastExpression) s);
            else if (s is FunctionCall)
                retstr += GenerateFunctionCall((FunctionCall) s);
            else if (s is VectorConstant)
                retstr += GenerateVectorConstant((VectorConstant) s);
            else if (s is RotationConstant)
                retstr += GenerateRotationConstant((RotationConstant) s);
            else if (s is ListConstant)
                retstr += GenerateListConstant((ListConstant) s);
            else if (s is Constant)
                retstr += GenerateConstant((Constant) s);
            else if (s is IdentDotExpression)
                retstr += Generate(CheckName(((IdentDotExpression) s).Name) + "." + ((IdentDotExpression) s).Member, s);
            else if (s is IdentExpression)
                retstr += Generate(CheckName(((IdentExpression) s).Name), s);
            else if (s is IDENT)
                retstr += Generate(CheckName(((TOKEN) s).yytext), s);
            else
            {
                foreach (SYMBOL kid in s.kids)
                    retstr += GenerateNode(kid);
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for a GlobalFunctionDefinition node.
        /// </summary>
        /// <param name="gf">The GlobalFunctionDefinition node.</param>
        /// <returns>String containing C# code for GlobalFunctionDefinition gf.</returns>
        private string GenerateGlobalFunctionDefinition(GlobalFunctionDefinition gf)
        {
            string retstr = String.Empty;

            // we need to separate the argument declaration list from other kids
            List<SYMBOL> argumentDeclarationListKids = new List<SYMBOL>();
            List<SYMBOL> remainingKids = new List<SYMBOL>();

            foreach (SYMBOL kid in gf.kids)
                if (kid is ArgumentDeclarationList)
                    argumentDeclarationListKids.Add(kid);
                else
                    remainingKids.Add(kid);

            retstr += GenerateIndented(String.Format("{0} {1}(", gf.ReturnType, CheckName(gf.Name)), gf);

            // print the state arguments, if any
            foreach (SYMBOL kid in argumentDeclarationListKids)
                retstr += GenerateArgumentDeclarationList((ArgumentDeclarationList) kid);

            retstr += GenerateLine(")");

            foreach (SYMBOL kid in remainingKids)
                retstr += GenerateNode(kid);

            return retstr;
        }

        /// <summary>
        /// Generates the code for a GlobalVariableDeclaration node.
        /// </summary>
        /// <param name="gv">The GlobalVariableDeclaration node.</param>
        /// <returns>String containing C# code for GlobalVariableDeclaration gv.</returns>
        private string GenerateGlobalVariableDeclaration(GlobalVariableDeclaration gv)
        {
            string retstr = String.Empty;

            foreach (SYMBOL s in gv.kids)
            {
                retstr += Indent();
                retstr += GenerateNode(s);
                retstr += GenerateLine(";");
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for a State node.
        /// </summary>
        /// <param name="s">The State node.</param>
        /// <returns>String containing C# code for State s.</returns>
        private string GenerateState(State s)
        {
            string retstr = String.Empty;

            foreach (SYMBOL kid in s.kids)
                if (kid is StateEvent)
                    retstr += GenerateStateEvent((StateEvent) kid, s.Name);

            return retstr;
        }

        /// <summary>
        /// Generates the code for a StateEvent node.
        /// </summary>
        /// <param name="se">The StateEvent node.</param>
        /// <param name="parentStateName">The name of the parent state.</param>
        /// <returns>String containing C# code for StateEvent se.</returns>
        private string GenerateStateEvent(StateEvent se, string parentStateName)
        {
            string retstr = String.Empty;

            // we need to separate the argument declaration list from other kids
            List<SYMBOL> argumentDeclarationListKids = new List<SYMBOL>();
            List<SYMBOL> remainingKids = new List<SYMBOL>();

            foreach (SYMBOL kid in se.kids)
                if (kid is ArgumentDeclarationList)
                    argumentDeclarationListKids.Add(kid);
                else
                    remainingKids.Add(kid);

            // "state" (function) declaration
            retstr += GenerateIndented(String.Format("public void {0}_event_{1}(", parentStateName, se.Name), se);

            // print the state arguments, if any
            foreach (SYMBOL kid in argumentDeclarationListKids)
                retstr += GenerateArgumentDeclarationList((ArgumentDeclarationList) kid);

            retstr += GenerateLine(")");

            foreach (SYMBOL kid in remainingKids)
                retstr += GenerateNode(kid);

            return retstr;
        }

        /// <summary>
        /// Generates the code for an ArgumentDeclarationList node.
        /// </summary>
        /// <param name="adl">The ArgumentDeclarationList node.</param>
        /// <returns>String containing C# code for ArgumentDeclarationList adl.</returns>
        private string GenerateArgumentDeclarationList(ArgumentDeclarationList adl)
        {
            string retstr = String.Empty;

            int comma = adl.kids.Count - 1; // tells us whether to print a comma

            foreach (Declaration d in adl.kids)
            {
                retstr += Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
                if (0 < comma--)
                    retstr += Generate(", ");
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for an ArgumentList node.
        /// </summary>
        /// <param name="al">The ArgumentList node.</param>
        /// <returns>String containing C# code for ArgumentList al.</returns>
        private string GenerateArgumentList(ArgumentList al)
        {
            string retstr = String.Empty;

            int comma = al.kids.Count - 1;  // tells us whether to print a comma

            foreach (SYMBOL s in al.kids)
            {
                retstr += GenerateNode(s);
                if (0 < comma--)
                    retstr += Generate(", ");
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for a CompoundStatement node.
        /// </summary>
        /// <param name="cs">The CompoundStatement node.</param>
        /// <returns>String containing C# code for CompoundStatement cs.</returns>
        private string GenerateCompoundStatement(CompoundStatement cs)
        {
            string retstr = String.Empty;

            // opening brace
            retstr += GenerateIndentedLine("{");
            m_braceCount++;

            foreach (SYMBOL kid in cs.kids)
                retstr += GenerateNode(kid);

            // closing brace
            m_braceCount--;
            retstr += GenerateIndentedLine("}");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a Declaration node.
        /// </summary>
        /// <param name="d">The Declaration node.</param>
        /// <returns>String containing C# code for Declaration d.</returns>
        private string GenerateDeclaration(Declaration d)
        {
            return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
        }

        /// <summary>
        /// Generates the code for a Statement node.
        /// </summary>
        /// <param name="s">The Statement node.</param>
        /// <returns>String containing C# code for Statement s.</returns>
        private string GenerateStatement(Statement s)
        {
            string retstr = String.Empty;
            bool printSemicolon = true;

            retstr += Indent();

            if (0 < s.kids.Count)
            {
                // Jump label prints its own colon, we don't need a semicolon.
                printSemicolon = !(s.kids.Top is JumpLabel);

                // If we encounter a lone Ident, we skip it, since that's a C#
                // (MONO) error.
                if (!(s.kids.Top is IdentExpression && 1 == s.kids.Count))
                    foreach (SYMBOL kid in s.kids)
                        retstr += GenerateNode(kid);
            }

            if (printSemicolon)
                retstr += GenerateLine(";");

            return retstr;
        }

        /// <summary>
        /// Generates the code for an Assignment node.
        /// </summary>
        /// <param name="a">The Assignment node.</param>
        /// <returns>String containing C# code for Assignment a.</returns>
        private string GenerateAssignment(Assignment a)
        {
            string retstr = String.Empty;

            List<string> identifiers = new List<string>();
            checkForMultipleAssignments(identifiers, a);

            retstr += GenerateNode((SYMBOL) a.kids.Pop());
            retstr += Generate(String.Format(" {0} ", a.AssignmentType), a);
            foreach (SYMBOL kid in a.kids)
                retstr += GenerateNode(kid);

            return retstr;
        }

        // This code checks for LSL of the following forms, and generates a
        // warning if it finds them.
        //
        // list l = [ "foo" ]; 
        // l = (l=[]) + l + ["bar"];
        // (produces l=["foo","bar"] in SL but l=["bar"] in OS)
        //
        // integer i;
        // integer j;
        // i = (j = 3) + (j = 4) + (j = 5);
        // (produces j=3 in SL but j=5 in OS)
        //
        // Without this check, that code passes compilation, but does not do what
        // the end user expects, because LSL in SL evaluates right to left instead
        // of left to right.
        //
        // The theory here is that producing an error and alerting the end user that
        // something needs to change is better than silently generating incorrect code.
        private void checkForMultipleAssignments(List<string> identifiers, SYMBOL s)
        {
            if (s is Assignment)
            {
                Assignment a = (Assignment)s;
                string newident = null;

                if (a.kids[0] is Declaration)
                {
                    newident = ((Declaration)a.kids[0]).Id;
                }
                else if (a.kids[0] is IDENT)
                {
                    newident = ((IDENT)a.kids[0]).yytext;
                }
                else if (a.kids[0] is IdentDotExpression)
                {
                    newident = ((IdentDotExpression)a.kids[0]).Name; // +"." + ((IdentDotExpression)a.kids[0]).Member;
                }
                else
                {
                    AddWarning(String.Format("Multiple assignments checker internal error '{0}' at line {1} column {2}.", a.kids[0].GetType(), ((SYMBOL)a.kids[0]).Line - 1, ((SYMBOL)a.kids[0]).Position));
                }

                if (identifiers.Contains(newident))
                {
                    AddWarning(String.Format("Multiple assignments to '{0}' at line {1} column {2}; results may differ between LSL and OSSL.", newident, ((SYMBOL)a.kids[0]).Line - 1, ((SYMBOL)a.kids[0]).Position));
                }
                identifiers.Add(newident);
            }

            int index;
            for (index = 0; index < s.kids.Count; index++)
            {
                checkForMultipleAssignments(identifiers, (SYMBOL) s.kids[index]);
            }
        }

        /// <summary>
        /// Generates the code for a ReturnStatement node.
        /// </summary>
        /// <param name="rs">The ReturnStatement node.</param>
        /// <returns>String containing C# code for ReturnStatement rs.</returns>
        private string GenerateReturnStatement(ReturnStatement rs)
        {
            string retstr = String.Empty;

            retstr += Generate("return ", rs);

            foreach (SYMBOL kid in rs.kids)
                retstr += GenerateNode(kid);

            return retstr;
        }

        /// <summary>
        /// Generates the code for a JumpLabel node.
        /// </summary>
        /// <param name="jl">The JumpLabel node.</param>
        /// <returns>String containing C# code for JumpLabel jl.</returns>
        private string GenerateJumpLabel(JumpLabel jl)
        {
            return Generate(String.Format("{0}:", CheckName(jl.LabelName)), jl) + " NoOp();\n";
        }

        /// <summary>
        /// Generates the code for a JumpStatement node.
        /// </summary>
        /// <param name="js">The JumpStatement node.</param>
        /// <returns>String containing C# code for JumpStatement js.</returns>
        private string GenerateJumpStatement(JumpStatement js)
        {
            return Generate(String.Format("goto {0}", CheckName(js.TargetName)), js);
        }

        /// <summary>
        /// Generates the code for an IfStatement node.
        /// </summary>
        /// <param name="ifs">The IfStatement node.</param>
        /// <returns>String containing C# code for IfStatement ifs.</returns>
        private string GenerateIfStatement(IfStatement ifs)
        {
            string retstr = String.Empty;

            retstr += GenerateIndented("if (", ifs);
            retstr += GenerateNode((SYMBOL) ifs.kids.Pop());
            retstr += GenerateLine(")");

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = ifs.kids.Top is Statement;
            if (indentHere) m_braceCount++;
            retstr += GenerateNode((SYMBOL) ifs.kids.Pop());
            if (indentHere) m_braceCount--;

            if (0 < ifs.kids.Count) // do it again for an else
            {
                retstr += GenerateIndentedLine("else", ifs);

                indentHere = ifs.kids.Top is Statement;
                if (indentHere) m_braceCount++;
                retstr += GenerateNode((SYMBOL) ifs.kids.Pop());
                if (indentHere) m_braceCount--;
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for a StateChange node.
        /// </summary>
        /// <param name="sc">The StateChange node.</param>
        /// <returns>String containing C# code for StateChange sc.</returns>
        private string GenerateStateChange(StateChange sc)
        {
            return Generate(String.Format("state(\"{0}\")", sc.NewState), sc);
        }

        /// <summary>
        /// Generates the code for a WhileStatement node.
        /// </summary>
        /// <param name="ws">The WhileStatement node.</param>
        /// <returns>String containing C# code for WhileStatement ws.</returns>
        private string GenerateWhileStatement(WhileStatement ws)
        {
            string retstr = String.Empty;

            retstr += GenerateIndented("while (", ws);
            retstr += GenerateNode((SYMBOL) ws.kids.Pop());
            retstr += GenerateLine(")");

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = ws.kids.Top is Statement;
            if (indentHere) m_braceCount++;
            retstr += GenerateNode((SYMBOL) ws.kids.Pop());
            if (indentHere) m_braceCount--;

            return retstr;
        }

        /// <summary>
        /// Generates the code for a DoWhileStatement node.
        /// </summary>
        /// <param name="dws">The DoWhileStatement node.</param>
        /// <returns>String containing C# code for DoWhileStatement dws.</returns>
        private string GenerateDoWhileStatement(DoWhileStatement dws)
        {
            string retstr = String.Empty;

            retstr += GenerateIndentedLine("do", dws);

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = dws.kids.Top is Statement;
            if (indentHere) m_braceCount++;
            retstr += GenerateNode((SYMBOL) dws.kids.Pop());
            if (indentHere) m_braceCount--;

            retstr += GenerateIndented("while (", dws);
            retstr += GenerateNode((SYMBOL) dws.kids.Pop());
            retstr += GenerateLine(");");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a ForLoop node.
        /// </summary>
        /// <param name="fl">The ForLoop node.</param>
        /// <returns>String containing C# code for ForLoop fl.</returns>
        private string GenerateForLoop(ForLoop fl)
        {
            string retstr = String.Empty;

            retstr += GenerateIndented("for (", fl);

            // It's possible that we don't have an assignment, in which case
            // the child will be null and we only print the semicolon.
            // for (x = 0; x < 10; x++)
            //      ^^^^^
            ForLoopStatement s = (ForLoopStatement) fl.kids.Pop();
            if (null != s)
            {
                retstr += GenerateForLoopStatement(s);
            }
            retstr += Generate("; ");
            // for (x = 0; x < 10; x++)
            //             ^^^^^^
            retstr += GenerateNode((SYMBOL) fl.kids.Pop());
            retstr += Generate("; ");
            // for (x = 0; x < 10; x++)
            //                     ^^^
            retstr += GenerateForLoopStatement((ForLoopStatement) fl.kids.Pop());
            retstr += GenerateLine(")");

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = fl.kids.Top is Statement;
            if (indentHere) m_braceCount++;
            retstr += GenerateNode((SYMBOL) fl.kids.Pop());
            if (indentHere) m_braceCount--;

            return retstr;
        }

        /// <summary>
        /// Generates the code for a ForLoopStatement node.
        /// </summary>
        /// <param name="fls">The ForLoopStatement node.</param>
        /// <returns>String containing C# code for ForLoopStatement fls.</returns>
        private string GenerateForLoopStatement(ForLoopStatement fls)
        {
            string retstr = String.Empty;

            int comma = fls.kids.Count - 1;  // tells us whether to print a comma

            // It's possible that all we have is an empty Ident, for example:
            //
            //     for (x; x < 10; x++) { ... }
            //
            // Which is illegal in C# (MONO). We'll skip it.
            if (fls.kids.Top is IdentExpression && 1 == fls.kids.Count)
                return retstr;

            for (int i = 0; i < fls.kids.Count; i++)
            {
                SYMBOL s = (SYMBOL)fls.kids[i];
                
                // Statements surrounded by parentheses in for loops
                //
                // e.g.  for ((i = 0), (j = 7); (i < 10); (++i))
                //
                // are legal in LSL but not in C# so we need to discard the parentheses
                //
                // The following, however, does not appear to be legal in LLS
                //
                // for ((i = 0, j = 7); (i < 10); (++i))
                //
                // As of Friday 20th November 2009, the Linden Lab simulators appear simply never to compile or run this
                // script but with no debug or warnings at all!  Therefore, we won't deal with this yet (which looks
                // like it would be considerably more complicated to handle).
                while (s is ParenthesisExpression)
                    s = (SYMBOL)s.kids.Pop();
                    
                retstr += GenerateNode(s);
                if (0 < comma--)
                    retstr += Generate(", ");
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for a BinaryExpression node.
        /// </summary>
        /// <param name="be">The BinaryExpression node.</param>
        /// <returns>String containing C# code for BinaryExpression be.</returns>
        private string GenerateBinaryExpression(BinaryExpression be)
        {
            string retstr = String.Empty;

            if (be.ExpressionSymbol.Equals("&&") || be.ExpressionSymbol.Equals("||"))
            {
                // special case handling for logical and/or, see Mantis 3174
                retstr += "((bool)(";
                retstr += GenerateNode((SYMBOL)be.kids.Pop());
                retstr += "))";
                retstr += Generate(String.Format(" {0} ", be.ExpressionSymbol.Substring(0,1)), be);
                retstr += "((bool)(";
                foreach (SYMBOL kid in be.kids)
                    retstr += GenerateNode(kid);
                retstr += "))";
            }
            else
            {
                retstr += GenerateNode((SYMBOL)be.kids.Pop());
                retstr += Generate(String.Format(" {0} ", be.ExpressionSymbol), be);
                foreach (SYMBOL kid in be.kids)
                    retstr += GenerateNode(kid);
            }

            return retstr;
        }

        /// <summary>
        /// Generates the code for a UnaryExpression node.
        /// </summary>
        /// <param name="ue">The UnaryExpression node.</param>
        /// <returns>String containing C# code for UnaryExpression ue.</returns>
        private string GenerateUnaryExpression(UnaryExpression ue)
        {
            string retstr = String.Empty;

            retstr += Generate(ue.UnarySymbol, ue);
            retstr += GenerateNode((SYMBOL) ue.kids.Pop());

            return retstr;
        }

        /// <summary>
        /// Generates the code for a ParenthesisExpression node.
        /// </summary>
        /// <param name="pe">The ParenthesisExpression node.</param>
        /// <returns>String containing C# code for ParenthesisExpression pe.</returns>
        private string GenerateParenthesisExpression(ParenthesisExpression pe)
        {
            string retstr = String.Empty;

            retstr += Generate("(");
            foreach (SYMBOL kid in pe.kids)
                retstr += GenerateNode(kid);
            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a IncrementDecrementExpression node.
        /// </summary>
        /// <param name="ide">The IncrementDecrementExpression node.</param>
        /// <returns>String containing C# code for IncrementDecrementExpression ide.</returns>
        private string GenerateIncrementDecrementExpression(IncrementDecrementExpression ide)
        {
            string retstr = String.Empty;

            if (0 < ide.kids.Count)
            {
                IdentDotExpression dot = (IdentDotExpression) ide.kids.Top;
                retstr += Generate(String.Format("{0}", ide.PostOperation ? CheckName(dot.Name) + "." + dot.Member + ide.Operation : ide.Operation + CheckName(dot.Name) + "." + dot.Member), ide);
            }
            else
                retstr += Generate(String.Format("{0}", ide.PostOperation ? CheckName(ide.Name) + ide.Operation : ide.Operation + CheckName(ide.Name)), ide);

            return retstr;
        }

        /// <summary>
        /// Generates the code for a TypecastExpression node.
        /// </summary>
        /// <param name="te">The TypecastExpression node.</param>
        /// <returns>String containing C# code for TypecastExpression te.</returns>
        private string GenerateTypecastExpression(TypecastExpression te)
        {
            string retstr = String.Empty;

            // we wrap all typecasted statements in parentheses
            retstr += Generate(String.Format("({0}) (", te.TypecastType), te);
            retstr += GenerateNode((SYMBOL) te.kids.Pop());
            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a FunctionCall node.
        /// </summary>
        /// <param name="fc">The FunctionCall node.</param>
        /// <returns>String containing C# code for FunctionCall fc.</returns>
        private string GenerateFunctionCall(FunctionCall fc)
        {
            string retstr = String.Empty;

            retstr += Generate(String.Format("{0}(", CheckName(fc.Id)), fc);

            foreach (SYMBOL kid in fc.kids)
                retstr += GenerateNode(kid);

            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a Constant node.
        /// </summary>
        /// <param name="c">The Constant node.</param>
        /// <returns>String containing C# code for Constant c.</returns>
        private string GenerateConstant(Constant c)
        {
            string retstr = String.Empty;

            // Supprt LSL's weird acceptance of floats with no trailing digits
            // after the period. Turn float x = 10.; into float x = 10.0;
            if ("LSL_Types.LSLFloat" == c.Type)
            {
                int dotIndex = c.Value.IndexOf('.') + 1;
                if (0 < dotIndex && (dotIndex == c.Value.Length || !Char.IsDigit(c.Value[dotIndex])))
                    c.Value = c.Value.Insert(dotIndex, "0");
                c.Value = "new LSL_Types.LSLFloat("+c.Value+")";
            }
            else if ("LSL_Types.LSLInteger" == c.Type)
            {
                c.Value = "new LSL_Types.LSLInteger("+c.Value+")";
            }
            else if ("LSL_Types.LSLString" == c.Type)
            {
                c.Value = "new LSL_Types.LSLString(\""+c.Value+"\")";
            }

            retstr += Generate(c.Value, c);

            return retstr;
        }

        /// <summary>
        /// Generates the code for a VectorConstant node.
        /// </summary>
        /// <param name="vc">The VectorConstant node.</param>
        /// <returns>String containing C# code for VectorConstant vc.</returns>
        private string GenerateVectorConstant(VectorConstant vc)
        {
            string retstr = String.Empty;

            retstr += Generate(String.Format("new {0}(", vc.Type), vc);
            retstr += GenerateNode((SYMBOL) vc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL) vc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL) vc.kids.Pop());
            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a RotationConstant node.
        /// </summary>
        /// <param name="rc">The RotationConstant node.</param>
        /// <returns>String containing C# code for RotationConstant rc.</returns>
        private string GenerateRotationConstant(RotationConstant rc)
        {
            string retstr = String.Empty;

            retstr += Generate(String.Format("new {0}(", rc.Type), rc);
            retstr += GenerateNode((SYMBOL) rc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL) rc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL) rc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL) rc.kids.Pop());
            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a ListConstant node.
        /// </summary>
        /// <param name="lc">The ListConstant node.</param>
        /// <returns>String containing C# code for ListConstant lc.</returns>
        private string GenerateListConstant(ListConstant lc)
        {
            string retstr = String.Empty;

            retstr += Generate(String.Format("new {0}(", lc.Type), lc);

            foreach (SYMBOL kid in lc.kids)
                retstr += GenerateNode(kid);

            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Prints a newline.
        /// </summary>
        /// <returns>A newline.</returns>
        private string GenerateLine()
        {
            return GenerateLine("");
        }

        /// <summary>
        /// Prints text, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>String s followed by newline.</returns>
        private string GenerateLine(string s)
        {
            return GenerateLine(s, null);
        }

        /// <summary>
        /// Prints text, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>String s followed by newline.</returns>
        private string GenerateLine(string s, SYMBOL sym)
        {
            string retstr = Generate(s, sym) + "\n";

            m_CSharpLine++;
            m_CSharpCol = 1;

            return retstr;
        }

        /// <summary>
        /// Prints text.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>String s.</returns>
        private string Generate(string s)
        {
            return Generate(s, null);
        }

        /// <summary>
        /// Prints text.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>String s.</returns>
        private string Generate(string s, SYMBOL sym)
        {
            if (null != sym)
                m_positionMap.Add(new KeyValuePair<int, int>(m_CSharpLine, m_CSharpCol), new KeyValuePair<int, int>(sym.Line, sym.Position));

            m_CSharpCol += s.Length;

            return s;
        }

        /// <summary>
        /// Prints text correctly indented, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>Properly indented string s followed by newline.</returns>
        private string GenerateIndentedLine(string s)
        {
            return GenerateIndentedLine(s, null);
        }

        /// <summary>
        /// Prints text correctly indented, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>Properly indented string s followed by newline.</returns>
        private string GenerateIndentedLine(string s, SYMBOL sym)
        {
            string retstr = GenerateIndented(s, sym) + "\n";

            m_CSharpLine++;
            m_CSharpCol = 1;

            return retstr;
        }

        /// <summary>
        /// Prints text correctly indented.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>Properly indented string s.</returns>
        //private string GenerateIndented(string s)
        //{
        //    return GenerateIndented(s, null);
        //}
        // THIS FUNCTION IS COMMENTED OUT TO SUPPRESS WARNINGS

        /// <summary>
        /// Prints text correctly indented.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>Properly indented string s.</returns>
        private string GenerateIndented(string s, SYMBOL sym)
        {
            string retstr = Indent() + s;

            if (null != sym)
                m_positionMap.Add(new KeyValuePair<int, int>(m_CSharpLine, m_CSharpCol), new KeyValuePair<int, int>(sym.Line, sym.Position));

            m_CSharpCol += s.Length;

            return retstr;
        }

        /// <summary>
        /// Prints correct indentation.
        /// </summary>
        /// <returns>Indentation based on brace count.</returns>
        private string Indent()
        {
            string retstr = String.Empty;

            for (int i = 0; i < m_braceCount; i++)
                for (int j = 0; j < m_indentWidth; j++)
                {
                     retstr += " ";
                     m_CSharpCol++;
                }

            return retstr;
        }

        /// <summary>
        /// Returns the passed name with an underscore prepended if that name is a reserved word in C#
        /// and not resevered in LSL otherwise it just returns the passed name.
        ///
        /// This makes no attempt to cache the results to minimise future lookups. For a non trivial
        /// scripts the number of unique identifiers could easily grow to the size of the reserved word
        /// list so maintaining a list or dictionary and doing the lookup there firstwould probably not
        /// give any real speed advantage.
        ///
        /// I believe there is a class Microsoft.CSharp.CSharpCodeProvider that has a function
        /// CreateValidIdentifier(str) that will return either the value of str if it is not a C#
        /// key word or "_"+str if it is. But availability under Mono?
        /// </summary>
        private string CheckName(string s)
        {
            if (CSReservedWords.IsReservedWord(s))
                return "@" + s;
            else
                return s;
        }
    }
}
