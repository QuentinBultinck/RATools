﻿using RATools.Data;
using RATools.Parser.Expressions;
using RATools.Parser.Expressions.Trigger;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RATools.Parser.Functions
{
    internal class LeaderboardFunction : FunctionDefinitionExpression
    {
        public LeaderboardFunction()
            : base("leaderboard")
        {
            Parameters.Add(new VariableDefinitionExpression("title"));
            Parameters.Add(new VariableDefinitionExpression("description"));
            Parameters.Add(new VariableDefinitionExpression("start"));
            Parameters.Add(new VariableDefinitionExpression("cancel"));
            Parameters.Add(new VariableDefinitionExpression("submit"));
            Parameters.Add(new VariableDefinitionExpression("value"));
            Parameters.Add(new VariableDefinitionExpression("format"));
            Parameters.Add(new VariableDefinitionExpression("lower_is_better"));

            DefaultParameters["format"] = new StringConstantExpression("value");
            DefaultParameters["lower_is_better"] = new BooleanConstantExpression(false);

            // additional parameters generated by dumper
            Parameters.Add(new VariableDefinitionExpression("id"));
            DefaultParameters["id"] = new IntegerConstantExpression(0);
        }

        public override bool Evaluate(InterpreterScope scope, out ExpressionBase result)
        {
            var leaderboard = new Leaderboard();

            var stringExpression = GetStringParameter(scope, "title", out result);
            if (stringExpression == null)
                return false;
            leaderboard.Title = stringExpression.Value;

            stringExpression = GetStringParameter(scope, "description", out result);
            if (stringExpression == null)
                return false;
            leaderboard.Description = stringExpression.Value;

            var context = scope.GetContext<AchievementScriptContext>();
            Debug.Assert(context != null);
            var serializationContext = context.SerializationContext;

            leaderboard.Start = ProcessTrigger(scope, "start", serializationContext, out result);
            if (leaderboard.Start == null)
                return false;

            leaderboard.Cancel = ProcessTrigger(scope, "cancel", serializationContext, out result);
            if (leaderboard.Cancel == null)
                return false;

            leaderboard.Submit = ProcessTrigger(scope, "submit", serializationContext, out result);
            if (leaderboard.Submit == null)
                return false;

            leaderboard.Value = ProcessValue(scope, "value", serializationContext, out result);
            if (leaderboard.Value == null)
                return false;

            var format = GetStringParameter(scope, "format", out result);
            if (format == null)
                return false;

            leaderboard.Format = Leaderboard.ParseFormat(format.Value);
            if (leaderboard.Format == ValueFormat.None)
            {
                result = new ErrorExpression(format.Value + " is not a supported leaderboard format", format);
                return false;
            }

            var lowerIsBetter = GetBooleanParameter(scope, "lower_is_better", out result);
            if (lowerIsBetter == null)
                return false;
            leaderboard.LowerIsBetter = lowerIsBetter.Value;

            var integerExpression = GetIntegerParameter(scope, "id", out result);
            if (integerExpression == null)
                return false;
            leaderboard.Id = integerExpression.Value;

            if (leaderboard.Submit == leaderboard.Start)
            {
                // if the submit trigger is true in the same frame that the start trigger activates,
                // the achievement will automatically submit. therefore, if the submit trigger is the
                // same as the start trigger, it will automatically submit and we can just replace
                // the submit trigger with always_true();
                ErrorExpression error;
                leaderboard.Submit = TriggerBuilder.BuildTrigger(new AlwaysTrueExpression(), out error);
            }

            int sourceLine = 0;
            var functionCall = scope.GetContext<FunctionCallExpression>();
            if (functionCall != null && functionCall.FunctionName.Name == this.Name.Name)
                sourceLine = functionCall.Location.Start.Line;

            context.Leaderboards[leaderboard] = sourceLine;
            return true;
        }

        private Trigger ProcessTrigger(InterpreterScope scope, string parameter, SerializationContext serializationContext, out ExpressionBase result)
        {
            var expression = GetRequirementParameter(scope, parameter, out result);
            if (expression == null)
                return null;

            ErrorExpression error;
            var trigger = TriggerBuilder.BuildTrigger(expression, out error);
            result = error;
            return trigger;
        }

        private static Value ProcessValue(InterpreterScope scope, string parameter, SerializationContext serializationContext, out ExpressionBase result)
        {
            var expression = GetParameter(scope, parameter, out result);
            if (expression == null)
                return null;

            ErrorExpression error;
            var value = ValueBuilder.BuildValue(expression, out error);
            result = error;
            return value;
        }
    }
}
