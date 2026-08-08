using System;
using System.Collections.Generic;

namespace Neurohard.Playwright.Io
{
    public static class GraphReader
    {
        private const string VarRefKey = "$var";

        public static DialogueGraph FromJson(string json)
        {
            var root = JsonParser.Parse(json).AsObject("La raíz del grafo");
            var graph = new DialogueGraph();

            graph.Version = root.Has("version") ? (int)root["version"].AsNumber("version") : 1;
            if (graph.Version != 1)
                throw new GraphFormatException(
                    $"Versión de formato {graph.Version} no soportada (se esperaba 1).", root["version"].Line);

            var nodesJson = (root["nodes"] ?? throw new GraphFormatException("Falta la propiedad 'nodes'.", root.Line))
                .AsArray("nodes");

            foreach (var nodeJson in nodesJson)
                graph.Add(ReadNode(nodeJson.AsObject("Cada nodo")));

            if (root.Has("start")) graph.Start = root["start"].AsString("start");
            else if (nodesJson.Count > 0) graph.Start = graph.Nodes[0].Id;

            return graph;
        }

        private static GraphNode ReadNode(JsonValue json)
        {
            var id = (json["id"] ?? throw new GraphFormatException("Un nodo no tiene 'id'.", json.Line))
                .AsString("El id del nodo");

            var node = new GraphNode
            {
                Id = id,
                Title = json.Has("title") ? json["title"].AsString($"El título de '{id}'") : null,
                Type = ReadNodeType(json["type"], id, json.Line)
            };

            if (json.Has("line")) node.Line = ReadLine(json["line"], id);

            if (json.Has("fallthrough"))
            {
                var mode = json["fallthrough"].AsString($"El fallthrough de '{id}'");
                node.Fallthrough = mode == "end" ? FallthroughMode.End
                    : mode == "error" ? FallthroughMode.Error
                    : throw new GraphFormatException(
                        $"Fallthrough desconocido '{mode}' en '{id}'. Usa \"end\" o \"error\".", json["fallthrough"].Line);
            }

            if (json.Has("editor"))
            {
                var e = json["editor"].AsObject($"El bloque editor de '{id}'");
                node.Editor.X = e.Has("x") ? (float)e["x"].AsNumber("editor.x") : 0f;
                node.Editor.Y = e.Has("y") ? (float)e["y"].AsNumber("editor.y") : 0f;
            }

            if (json.Has("out"))
                foreach (var edgeJson in json["out"].AsArray($"Las salidas de '{id}'"))
                    node.Out.Add(ReadEdge(edgeJson.AsObject($"Cada salida de '{id}'"), id));

            return node;
        }

        private static NodeType ReadNodeType(JsonValue json, string nodeId, int fallbackLine)
        {
            if (json == null)
                throw new GraphFormatException($"El nodo '{nodeId}' no declara 'type'.", fallbackLine);

            switch (json.AsString($"El tipo de '{nodeId}'"))
            {
                case "line": return NodeType.Line;
                case "choice": return NodeType.Choice;
                case "hub": return NodeType.Hub;
                default:
                    throw new GraphFormatException(
                        $"Tipo de nodo desconocido en '{nodeId}'. Usa \"line\", \"choice\" o \"hub\".", json.Line);
            }
        }

        /// <summary>Lee un valor literal o una referencia { "$var": "nombre" }.</summary>
        private static object ReadValue(JsonValue json, string context)
        {
            if (json == null) return null;

            if (json.Type == JsonValue.Kind.Object)
            {
                var name = json[VarRefKey];
                if (name == null)
                    throw new GraphFormatException(
                        $"{context}: un objeto como valor solo se admite en la forma {{ \"{VarRefKey}\": \"nombre\" }}.",
                        json.Line);

                return new VariableRef(name.AsString($"{context}: el nombre de la referencia"));
            }

            return json.AsLoose();
        }

        private static GraphLine ReadLine(JsonValue json, string nodeId)
        {
            json.AsObject($"La línea de '{nodeId}'");
            var line = new GraphLine
            {
                Text = json.Has("text") ? json["text"].AsString($"El texto de '{nodeId}'") : null,
                LineId = json.Has("lineId") ? json["lineId"].AsString($"El lineId de '{nodeId}'") : null,
                Speaker = json.Has("speaker") ? json["speaker"].AsString($"El hablante de '{nodeId}'") : null
            };

            if (string.IsNullOrEmpty(line.Text) && string.IsNullOrEmpty(line.LineId))
                throw new GraphFormatException(
                    $"Una línea de '{nodeId}' no tiene ni 'text' ni 'lineId'.", json.Line);

            if (json.Has("tags"))
                foreach (var tag in json["tags"].AsArray($"Los tags de '{nodeId}'"))
                    line.Tags.Add(tag.AsString("Cada tag"));

            return line;
        }

        private static GraphEdge ReadEdge(JsonValue json, string nodeId)
        {
            var edge = new GraphEdge
            {
                To = json.Has("to") ? json["to"].AsString($"El destino de una salida de '{nodeId}'") : null,
                OptionId = json.Has("id") ? json["id"].AsString($"El id de opción en '{nodeId}'") : null,
                Reason = json.Has("reason") ? json["reason"].AsString($"El motivo de una opción de '{nodeId}'") : null,
                HideWhenUnavailable = json.Has("hideWhenUnavailable") &&
                                      json["hideWhenUnavailable"].AsBool("hideWhenUnavailable")
            };

            if (json.Has("line")) edge.Line = ReadLine(json["line"], nodeId);
            if (json.Has("when")) edge.When = ReadCondition(json["when"], nodeId);

            if (json.Has("then"))
                foreach (var effectJson in json["then"].AsArray($"Los efectos de '{nodeId}'"))
                    edge.Then.Add(ReadEffect(effectJson.AsObject($"Cada efecto de '{nodeId}'"), nodeId));

            return edge;
        }

        private static Condition ReadCondition(JsonValue json, string nodeId)
        {
            json.AsObject($"Una condición de '{nodeId}'");

            if (json.Has("all")) return new Condition.All(ReadConditionList(json["all"], nodeId));
            if (json.Has("any")) return new Condition.Any(ReadConditionList(json["any"], nodeId));
            if (json.Has("not")) return new Condition.Not(ReadCondition(json["not"], nodeId));
            if (json.Has("query"))
            {
                var name = json["query"].AsString($"El nombre de consulta en '{nodeId}'");

                var args = new List<string>();
                if (json.Has("args"))
                    foreach (var a in json["args"].AsArray($"Los argumentos de la consulta '{name}'"))
                        args.Add(a.Type == JsonValue.Kind.String ? a.StringValue : a.ToString());

                var queryOp = json.Has("op")
                    ? ParseComparison(json["op"].AsString("El operador"), json["op"].Line)
                    : ComparisonOp.Equal;

                var queryValue = json.Has("value")
                    ? ReadValue(json["value"], $"El valor de una consulta de '{nodeId}'")
                    : null;

                return new Condition.Query(name, args, queryOp, queryValue);
            }

            var variable = (json["var"] ?? throw new GraphFormatException(
                $"Una condición de '{nodeId}' no tiene 'var', 'all', 'any' ni 'not'.", json.Line))
                .AsString("El nombre de variable");

            var opText = json.Has("op") ? json["op"].AsString("El operador") : "==";
            var op = ParseComparison(opText, json.Line);

            object value = json.Has("value")
    ? ReadValue(json["value"], $"El valor de una condición de '{nodeId}'")
    : null;
            return new Condition.Compare(variable, op, value);
        }

        private static List<Condition> ReadConditionList(JsonValue json, string nodeId)
        {
            var list = new List<Condition>();
            foreach (var item in json.AsArray($"Una lista de condiciones de '{nodeId}'"))
                list.Add(ReadCondition(item, nodeId));
            return list;
        }

        private static ComparisonOp ParseComparison(string op, int line)
        {
            switch (op)
            {
                case "==": return ComparisonOp.Equal;
                case "!=": return ComparisonOp.NotEqual;
                case ">": return ComparisonOp.Greater;
                case ">=": return ComparisonOp.GreaterOrEqual;
                case "<": return ComparisonOp.Less;
                case "<=": return ComparisonOp.LessOrEqual;
                case "exists": return ComparisonOp.Exists;
                case "!exists": return ComparisonOp.NotExists;
                default:
                    throw new GraphFormatException(
                        $"Operador desconocido '{op}'. Usa ==, !=, >, >=, <, <=, exists o !exists.", line);
            }
        }

        private static Effect ReadEffect(JsonValue json, string nodeId)
        {
            if (json.Has("command"))
            {
                var name = json["command"].AsString($"El comando de '{nodeId}'");
                var args = new List<string>();
                if (json.Has("args"))
                    foreach (var a in json["args"].AsArray($"Los argumentos de '{name}'"))
                        args.Add(a.Type == JsonValue.Kind.String ? a.StringValue : a.ToString());
                return new Effect.Command(name, args);
            }

            var variable = (json["var"] ?? throw new GraphFormatException(
                $"Un efecto de '{nodeId}' no tiene ni 'var' ni 'command'.", json.Line))
                .AsString("El nombre de variable");

            var opText = json.Has("op") ? json["op"].AsString("El operador") : "=";
            var op = opText switch
            {
                "=" => AssignOp.Set,
                "+=" => AssignOp.Add,
                "-=" => AssignOp.Subtract,
                _ => throw new GraphFormatException(
                    $"Operador de asignación desconocido '{opText}'. Usa =, += o -=.", json.Line)
            };

            return new Effect.Assign(variable, op,
                json.Has("value") ? ReadValue(json["value"], $"El valor de un efecto de '{nodeId}'") : null);
        }
    }
}