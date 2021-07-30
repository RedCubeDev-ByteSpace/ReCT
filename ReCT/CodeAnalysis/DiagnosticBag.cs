using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ReCT.CodeAnalysis.Symbols;
using ReCT.CodeAnalysis.Syntax;
using ReCT.CodeAnalysis.Text;
using Mono.Cecil;

namespace ReCT.CodeAnalysis
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(DiagnosticBag diagnostics)
        {
            _diagnostics.AddRange(diagnostics._diagnostics);
        }

        public void Report(TextLocation location, string message)
        {
            var diagnostic = new Diagnostic(location, message);
            _diagnostics.Add(diagnostic);
        }

        public void removeRangeBefore(string message)
        {
            var remove = new Queue<Diagnostic>();
            
            for (int i = _diagnostics.Count - 1; i >= 0; i--)
            {
                remove.Enqueue(_diagnostics[i]);

                if (_diagnostics[i].Message == message)
                    break;
            }

            while (remove.Count > 0)
            {
                _diagnostics.Remove(remove.Dequeue());
            }
        }

        public void ReportInvalidNumber(TextLocation location, string text, TypeSymbol type)
        {
            var message = $"The number {text} isn't valid {type}.";
            Report(location, message);
        }

        internal void ReportInvalidHexNumber(TextLocation location, string text)
        {
            var message = $"The number {text} isn't valid Hex.";
            Report(location, message);
        }

        internal void ReportInvalidBinaryNumber(TextLocation location, string text)
        {
            var message = $"The number {text} isn't valid Binary.";
            Report(location, message);
        }

        public void ReportBadCharacter(TextLocation location, char character)
        {
            var message = $"Bad character input: '{character}'.";
            Report(location, message);
        }

        public void ReportUnterminatedString(TextLocation location)
        {
            var message = "Unterminated string literal.";
            Report(location, message);
        }

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            var message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>.";
            Report(location, message);
        }

        public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol operandType)
        {
            var message = $"Unary operator '{operatorText}' is not defined for type '{operandType}'.";
            Report(location, message);
        }

        public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            var message = $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'.";
            Report(location, message);
        }

        public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
        {
            var message = $"A parameter with the name '{parameterName}' already exists.";
            Report(location, message);
        }

        public void ReportUndefinedVariable(TextLocation location, string name)
        {
            var message = $"Variable '{name}' doesn't exist.";
            Report(location, message);
        }

        public void ReportNotAVariable(TextLocation location, string name)
        {
            var message = $"'{name}' is not a variable.";
            Report(location, message);
        }

        public void ReportUndefinedType(TextLocation location, string name)
        {
            var message = $"Type '{name}' doesn't exist.";
            Report(location, message);
        }

        public void ReportCannotConvert(TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'.";
            Report(location, message);
        }

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol fromType, TypeSymbol toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?)";
            Report(location, message);
        }

        public void ReportSymbolAlreadyDeclared(TextLocation location, string name)
        {
            var message = $"'{name}' is already declared.";
            Report(location, message);
        }

        public void ReportCannotAssign(TextLocation location, string name)
        {
            var message = $"Variable '{name}' is read-only and cannot be assigned to.";
            Report(location, message);
        }

        public void ReportUndefinedFunction(TextLocation location, string name)
        {
            var message = $"Function '{name}' doesn't exist.";
            Report(location, message);
        }

        public void ReportNotAFunction(TextLocation location, string name)
        {
            var message = $"'{name}' is not a function.";
            Report(location, message);
        }

        public void ReportWrongArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
        {
            var message = $"Function '{name}' requires {expectedCount} arguments but was given {actualCount}.";
            Report(location, message);
        }

        internal void ReportFunctionCantBeAbstract(TextLocation location)
        {
            var message = $"Functions arent allowed to be Abstract! (if you are looking to override use 'virt' instead)";
            Report(location, message);
        }

        internal void ReportMemberCantUseModifier(TextLocation location, string v, string text)
        {
            Report(location, $"The current '{v}' Member cant use the '{text}' modifier!");
        }

        internal void ReportMemberAlreadyRecieved(TextLocation location, string text)
        {
            Report(location, $"The current Member already has the '{text}' modifier!");
        }

        public void ReportExpressionMustHaveValue(TextLocation location)
        {
            var message = "Expression must have a value.";
            Report(location, message);
        }

        public void ReportInvalidBreakOrContinue(TextLocation location, string text)
        {
            var message = $"The keyword '{text}' can only be used inside of loops.";
            Report(location, message);
        }

        public void ReportAllPathsMustReturn(TextLocation location)
        {
            var message = "Not all code paths return a value.";
            Report(location, message);
        }

        public void ReportInvalidReturnExpression(TextLocation location, string functionName)
        {
            var message = $"Since the function '{functionName}' does not return a value the 'return' keyword cannot be followed by an expression.";
            Report(location, message);
        }

        public void ReportInvalidReturnWithValueInGlobalStatements(TextLocation location)
        {
            var message = "The 'return' keyword cannot be followed by an expression in global statements.";
            Report(location, message);
        }

        public void ReportMissingReturnExpression(TextLocation location, TypeSymbol returnType)
        {
            var message = $"An expression of type '{returnType}' is expected.";
            Report(location, message);
        }

        public void ReportInvalidExpressionStatement(TextLocation location)
        {
            var message = $"Only assignment and call expressions can be used as a statement.";
            Report(location, message);
        }

        public void ReportOnlyOneFileCanHaveGlobalStatements(TextLocation location)
        {
            var message = $"At most one file can have global statements.";
            Report(location, message);
        }

        public void ReportMainMustHaveCorrectSignature(TextLocation location)
        {
            var message = $"main must not take arguments and not return anything.";
            Report(location, message);
        }

        public void ReportCannotMixMainAndGlobalStatements(TextLocation location)
        {
            var message = $"Cannot declare main function when global statements are used.";
            Report(location, message);
        }

        public void ReportInvalidReference(string path)
        {
            var message = $"The reference is not a valid .NET assembly: '{path}'";
            Report(default, message);
        }

        public void ReportRequiredTypeNotFound(string minskName, string metadataName)
        {
            var message = minskName == null
                ? $"The required type '{metadataName}' cannot be resolved among the given references."
                : $"The required type '{minskName}' ('{metadataName}') cannot be resolved among the given references.";
            Report(default, message);
        }

        public void ReportRequiredTypeAmbiguous(string minskName, string metadataName, TypeDefinition[] foundTypes)
        {
            var assemblyNames = foundTypes.Select(t => t.Module.Assembly.Name.Name);
            var assemblyNameList = string.Join(", ", assemblyNames);
            var message = minskName == null
                ? $"The required type '{metadataName}' was found in multiple references: {assemblyNameList}."
                : $"The required type '{minskName}' ('{metadataName}') was found in multiple references: {assemblyNameList}.";
            Report(default, message);
        }

        public void ReportRequiredMethodNotFound(string typeName, string methodName, string[] parameterTypeNames)
        {
            var parameterTypeNameList = string.Join(", ", parameterTypeNames);
            var message = $"The required method '{typeName}.{methodName}({parameterTypeNameList})' cannot be resolved among the given references.";
            Report(default, message);
        }

        internal void ReportCustomeMessage(string text)
        {
            Report(default, text);
        }

        internal void ReportUnknownPackage(string package)
        {
            Report(default, $"Couldnt find Package '{package}'!");
        }

        internal void ReportNamespaceNotFound(TextLocation location, string @namespace)
        {
            Report(location, $"Couldnt find Namespace / Package '{@namespace}'! Are you missing a Package?");
        }

        internal void NamespaceCantBeUsedTwice(TextLocation location, string text)
        {
            Report(location, $"Cant 'use' Namespace '{text}' multiple times!");
        }

        internal void ReportClassNotFound(TextLocation location, string text)
        {
            Report(location, $"Class '{text}' doesnt exist!");
        }

        internal void ReportWrongNumberOfConstructorArgs(TextLocation location, string text, int exp, int count)
        {
            Report(location, $"Constructor of Class '{text}' exprected {exp} parameters but got {count}!");
        }

        internal void ReportFunctionNotFoundInObject(TextLocation location, string text, string name)
        {
            Report(location, $"Function '{text}' couldnt be found in Class '{name}'!");
        }

        internal void ReportFunctionInObjectHasDifferentParams(TextLocation location, string text, string name, int count)
        {
            Report(location, $"Function '{text}' in Class '{name}' does not take {count} Arguments!");
        }

        internal void ReportVariableNotFoundInObject(TextLocation location, string text, string name)
        {
            Report(location, $"Class '{name}' doesnt have a Variable called '{text}'! (Maybe its not accessable)");
        }

        internal void ReportCantMakeInstanceOfStaticClass(TextLocation location, string text)
        {
            Report(location, $"Can not make instance of static class '{text}'!");
        }

        internal void ReportInvalidEnumType(TextLocation location, string text)
        {
            Report(location, $"Can only use Integers in Enum '{text}'!");
        }

        internal void ReportInvalidEnumNames(TextLocation location, string text1, string text2)
        {
            Report(location, $"Name '{text2}' can only be used once in Enum '{text1}'!");
        }

        internal void ReportUnknownAccessSource(TextLocation location)
        {
            Report(location, $"Couldnt find the Value you were trying to Access!");
        }

        internal void ReportTypefunctionVarOnly(string name)
        {
            Report(default, $"Can only do Typefunction '{name}' on Variables!");
        }

        internal void ReportTypefunctionNotFound(TextLocation location, string text1, string text2)
        {
            Report(location, $"Couldnt find Typefunction '{text1}' for Datatype '{text2}'!");
        }

        internal void ReportClassSymbolNotFound(TextLocation location)
        {
            Report(location, $"Couldnt find Class to Access!");
        }

        internal void ReportCanOnlyGetFromEnum(TextLocation location, string name, string v)
        {
            Report(location, $"Enum '{name}' only supports Get Access! Tried to: '{v}'!");
        }

        internal void ReportVirtualFunctionInMain(TextLocation location)
        {
            Report(location, $"Cant create a Virtual function in the Main class!");
        }

        internal void ReportFunctionCantBeCalledMain(TextLocation location)
        {
            Report(location, $"Cant create a Function called 'main'! This function name is reserved!");
        }

        internal void ReportEnumMemberNotFound(TextLocation location, string name, string text)
        {
            Report(location, $"Enum '{name}' doesnt have a Member called '{text}'!");
        }

        internal void ReportAliasSourceMissing(TextLocation location, string text)
        {
            Report(location, $"Couldnt create Alias for Package '{text}' because it couldnt be found!");
        }

        internal void ReportAliasTargetAlreadyRegistered(TextLocation location, string text1, string text2)
        {
            Report(location, $"Couldnt Alias Package '{text1}' to '{text2}' because a Package called '{text2}' is already Registered!");
        }

        internal void ReportUnknownArrayLength(TextLocation location)
        {
            Report(location, $"Couldnt create Array because the Length is unknown!");
        }


        internal void ReportSymbolHasKeywordArr(TextLocation location, string v, string name)
        {
            Report(location, $"Couldnt create {v} '{name}' because its Name includes the reserved Phrase 'Arr'!");
        }

        internal void ReportWrongConditionType(TextLocation location, string name)
        {
            Report(location, $"Condition of Ternary Expression needs to be of Type 'Bool', instead got '{name}'!");
        }

        internal void ReportClassCantBeAbstractAndStatic(TextLocation location, string text)
        {
            Report(location, $"Class '{text}' cant be Abstract and Static at the same time!");
        }

        internal void ReportTernaryLeftAndRightTypesDontMatch(TextLocation location, string name1, string name2)
        {
            Report(location, $"Datatypes inside of Ternary Expression have to be equal! (Left was '{name1}'; Right was '{name2}')");
        }

        internal void ReportElementTypeDoesNotMatchArrayType(TextLocation location, string name1, string name2)
        {
            Report(location, $"Datatype of Element ('{name1}') does not match the Arrays Type ('{name2}')!");
        }

        internal void ReportCouldtFindClassToInheritFrom(TextLocation location, string text1, string text2)
        {
            Report(location, $"Couldnt Inherit class '{text1}' from class '{text2}'! Class '{text2}' couldnt be found!");
        }

        internal void ReportCantThreadFunctionWithArgs(TextLocation location, string text)
        {
            Report(location, $"Cant Thread Function '{text}' because it has Arguments!");
        }

        internal void ReportInheratingClassNeedsToBeAbstract(TextLocation location, string text)
        {
            Report(location, $"Cant inherit from Class '{text}' because its not Abstract!");
        }

        internal void ReportPrefixedWithUnknownTarget(TextLocation location)
        {
            Report(location, $"Received Modifiers with an unknown Target!");
        }

        internal void ReportCantInheritWithAbstractClass(TextLocation location, string text)
        {
            Report(location, $"Cant make Class '{text}' Abstract because its inherating!");
        }

        internal void ReportCantActionFunctionWithArgs(TextLocation location, string text)
        {
            Report(location, $"Cant create Action for Function '{text}' because it has Arguments!");
        }

        internal void ReportModifierInFunction(TextLocation location, string text)
        {
            Report(location, $"Cant use the '{text}' Modifier in a function!");
        }

        internal void ReportLocalVariableCantBeVirtual(TextLocation location)
        {
            Report(location, $"Local Variables cant be Virtual!");
        }

        internal void ReportVirtualVarInMain(TextLocation location)
        {
            Report(location, $"Cant create a Virtual variable in the Main class!");
        }

        internal void ReportCantMakeInstanceOfAbstractClass(TextLocation location, string text)
        {
            Report(location, $"Can not make instance of abstract class '{text}'!");
        }

        internal void ReportCantUseVirtVarInNormalClass(TextLocation location)
        {
            Report(location, $"Cant create Virtual Variable in Non-Abstract Class!");
        }

        internal void ReportCantUseVirtFuncInNormalClass(TextLocation location)
        {
            Report(location, $"Cant create Virtual Function in Non-Abstract Class!");
        }

        internal void ReportVirtualFunctionsNeedToBePublic(TextLocation location)
        {
            Report(location, $"Virtual Functions need to be Public!");
        }

        internal void ReportGloablClassVarsNeedToBePublic(TextLocation location, string text)
        {
            Report(location, $"Cant create Variable '{text}'. Only Public Variables are allowed to be in Global Space!");
        }

        internal void ReportBaseNotAllowedInMain(TextLocation location)
        {
            Report(location, $"The base Statement is not allowed in the Main Class!");
        }

        internal void ReportBaseConstructorCallRequired(TextLocation location, string text)
        {
            Report(location, $"Constructor of Inherating Class '{text}' needs to call 'base'! (its recommended to call it at the start of the Method)");
        }

        internal void ReportBaseNotAllowedInNonInheratingClass(TextLocation location)
        {
            Report(location, $"Base Statement isnt allowed in Non-Inherating Class!");
        }

        internal void ReportBaseNotAllowedInNonConstructorMethods(TextLocation location)
        {
            Report(location, $"Base Statement can only be used in the Constructor!");
        }

        internal void ReportWrongArgumentType(TextLocation location, string v, int i, string name0, string name1, string name2)
        {
            Report(location, $"Function '{v}' Argument {i} ('{name0}') needs to be of Type '{name1}'! Got '{name2}'!");
        }

        internal void ReportCantUseOvrFuncInNormalClass(TextLocation location)
        {
            Report(location, $"Cant use Override in a Non-Inherating Class!");
        }

        internal void ReportOverridingFunctionsNeedToBePublic(TextLocation location)
        {
            Report(location, $"Cant use Override with a Private Function!");
        }

        internal void ReportFunctionToOverrideNotFound(TextLocation location, string text)
        {
            Report(location, $"Cant override function '{text}' because it wasnt found in the Base Class!");
        }

        internal void ReportOverridingFunctionsParametersNeedToBeTheSame(TextLocation location, string text)
        {
            Report(location, $"Cant override function '{text}' because its Parameters dont match up with the one in the Base Class!");
        }

        internal void ReportOverridingFunctionsTypeNeedsToBeTheSame(TextLocation location, string text)
        {
            Report(location, $"Cant override function '{text}' because its Return-Type doesnt match up with the Return-Type of the one in the Base Class!");
        }

        internal void ReportLocalVariableCantBeOverride(TextLocation location)
        {
            Report(location, $"Local Variables cant be Overrides!");
        }

        internal void ReportOverrideVarInMain(TextLocation location)
        {
            Report(location, $"Cant create an Overriding variable in the Main class!");
        }

        internal void ReportCantUseOvrVarInNormalClass(TextLocation location)
        {
            Report(location, $"Cant create Overriding Variable in Non-Inherating Class!");
        }

        internal void ReportCantFindVarToOverride(TextLocation location, string text)
        {
            Report(location, $"Cant create overriding Variable '{text}' because it couldnt be found in the Base Class!");
        }

        internal void ReportCanOnlyActionVoids(TextLocation location, string text)
        {
            Report(location, $"Cant action Function '{text}' because it needs to be pf Return-Type 'void'!");
        }

        internal void ReportInstanceTestTypeNeedsToBeInheratingClass(TextLocation location, string text)
        {
            Report(location, $"Cant do IsInstance check with Type '{text}' because it needs to be an inherating Class!");
        }

        internal void ReportInstanceTestTypeNeedsToInherateFromSameClass(TextLocation location, string name1, string name2)
        {
            Report(location, $"Cant do IsInstance check with Type '{name1}' and '{name2}' because they arent connected through an Abstract Class!");
        }

        internal void ReportInstanceTestTypeNeedsToBeAbstractOrInheratingClass(TextLocation location, string name)
        {
            Report(location, $"Cant do IsInstance check with Type '{name}' because it needs to be an inherating or abstract Class!");
        }

        internal void ReportLambdaInLambda(TextLocation location)
        {
            Report(location, $"Unable to create Lambda Function inside of Lambda Function!");
        }
    }
}
