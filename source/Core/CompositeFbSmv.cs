﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FB2SMV.FBCollections;
using FB2SMV.ServiceClasses;

namespace FB2SMV
{
    namespace Core
    {
        internal class CompositeFbSmv
        {
            public static string FbInstances(IEnumerable<FBInstance> instances, IEnumerable<Event> nonFilteredEvents, IEnumerable<Variable> nonFilteredVariables, IEnumerable<Connection> connections, Settings settings)
            {
                string inst = "";
                foreach (FBInstance instance in instances)
                {
                    var instanceEvents = nonFilteredEvents.Where(ev => ev.FBType == instance.InstanceType);
                    var instanceVars = nonFilteredVariables.Where(v => v.FBType == instance.InstanceType);

                    string instanceParametersString = InstanceParametersString(instanceEvents, instanceVars, connections, instance.Name);

                    string instImplementation = instance.InstanceType + " (" + instanceParametersString + ")";

                    inst += String.Format(Smv.VarDeclarationBlock, instance.Name, (settings.UseProcesses ? "process " : "") + instImplementation);
                }
                return inst;
            }
            public static string InternalBuffersDeclaration(IEnumerable<FBInstance> instances, IEnumerable<Connection> connections, IEnumerable<Event> nonFilteredEvents, IEnumerable<Variable> nonFilteredVariables)
            {
                string buffers = "";
                foreach (FBInstance instance in instances)
                {
                    var instanceVariables = nonFilteredVariables.Where(v => v.FBType == instance.InstanceType && v.Direction != Direction.Internal);
                    var instanceEvents = nonFilteredEvents.Where(ev => ev.FBType == instance.InstanceType && ev.Direction != Direction.Internal);


                    foreach (Event ev in instanceEvents)
                    {
                        //Connection inputConnection;
                        //if (! _isInputFromComponent(ev, connections, instance.Name, out inputConnection))
                        //{
                        buffers += String.Format(Smv.VarDeclarationBlock, instance.Name + "_" + ev.Name, Smv.DataTypes.BoolType);
                        //}
                    }
                    foreach (Variable variable in instanceVariables)
                    {
                        Connection inputConnection;
                        if (!_isInputFromComponent(variable, connections, instance.Name, out inputConnection))
                        {
                            if (variable.ArraySize == 0)
                                buffers += String.Format(Smv.VarDeclarationBlock, instance.Name + "_" + variable.Name, variable.SmvType);
                            else
                            {
                                for (int i = 0; i < variable.ArraySize; i++)
                                {
                                    buffers += String.Format(Smv.VarDeclarationBlock, instance.Name + "_" + variable.Name + Smv.ArrayIndex(i), variable.SmvType);
                                }
                            }
                        }
                    }
                    buffers += String.Format(Smv.VarDeclarationBlock, instance.Name + "_" + Smv.Alpha, Smv.DataTypes.BoolType);
                    buffers += String.Format(Smv.VarDeclarationBlock, instance.Name + "_" + Smv.Beta, Smv.DataTypes.BoolType);
                    buffers += "\n";
                }
                return buffers;
            }
            private static bool _isInputFromComponent(FBInterface instanceParameter, IEnumerable<Connection> connections, string instanceName, out Connection inputConnection)
            {
                inputConnection = null;
                bool srcComponent = false;

                if (instanceParameter.Direction == Direction.Input)
                {
                    inputConnection = connections.FirstOrDefault(conn => conn.Destination == instanceName + "." + instanceParameter.Name);
                    if (inputConnection != null) Smv.ConvertConnectionVariableName(inputConnection.Source, Smv.ModuleParameters.Event, out srcComponent);
                }

                return srcComponent;
            }

            private static string _getInstanceParameterString(FBInterface instanceParameter, IEnumerable<Connection> connections, string instanceName)
            {
                Connection inputConnection;
                if (_isInputFromComponent(instanceParameter, connections, instanceName, out inputConnection))
                {
                    return (inputConnection.Source.Replace('.', '_'));
                }
                else
                {
                    return (instanceName + "_" + instanceParameter.Name);
                }
            }
            public static string InstanceParametersString(IEnumerable<Event> events, IEnumerable<Variable> variables, IEnumerable<Connection> connections, string instanceName)
            {
                string moduleParameters = "";
                foreach (Event ev in events)
                {
                    //moduleParameters += _getInstanceParameterString(ev, connections, instanceName) + Smv.ModuleParameters.Splitter;
                    moduleParameters += (instanceName + "_" + ev.Name) + Smv.ModuleParameters.Splitter;
                }
                foreach (Variable variable in variables)
                {
                    if (variable.Direction != Direction.Internal)
                    {
                        moduleParameters += _getInstanceParameterString(variable, connections, instanceName) + Smv.ModuleParameters.Splitter;
                    }
                }

                moduleParameters += instanceName + "_" + Smv.Alpha + Smv.ModuleParameters.Splitter;
                moduleParameters += instanceName + "_" + Smv.Beta + Smv.ModuleParameters.Splitter;

                return moduleParameters.TrimEnd(Smv.ModuleParameters.Splitter.ToCharArray());
            }

            private static string _clearZeroValues(string input, Type varType) //Fix the problem with 0.0 initial values
            {
                if (input == "0.0") return "0";
                if (varType == typeof (Smv.DataTypes.BoolSmvType))
                {
                    if (input == "0") return Smv.False;
                    if (input == "1") return Smv.True;
                }
                return input;
            }
            public static string InternalBuffersInitialization(IEnumerable<FBInstance> instances, IEnumerable<Connection> connections, IEnumerable<Event> nonFilteredEvents, IEnumerable<Variable> nonFilteredVariables, IEnumerable<InstanceParameter> instanceParameters)
            {
                string buffersInit = "";
                foreach (FBInstance instance in instances)
                {
                    var instanceVariables = nonFilteredVariables.Where(v => v.FBType == instance.InstanceType && v.Direction != Direction.Internal);
                    var instanceEvents = nonFilteredEvents.Where(ev => ev.FBType == instance.InstanceType && ev.Direction != Direction.Internal);
                    foreach (Event ev in instanceEvents)
                    {
                        //Connection inputConnection;
                        //if (!_isInputFromComponent(ev, connections, instance.Name, out inputConnection))
                        //{
                        buffersInit += String.Format(Smv.VarInitializationBlock, instance.Name + "_" + ev.Name, Smv.False);
                        //}
                    }
                    foreach (Variable variable in instanceVariables)
                    {
                        Connection inputConnection;
                        if (_isInputFromComponent(variable, connections, instance.Name, out inputConnection)) continue;

                        // if(! _isInputFromComponent)
                        InstanceParameter instanceParameter = instanceParameters.FirstOrDefault(p => p.InstanceName == instance.Name && p.Name == variable.Name);
                        if (variable.ArraySize == 0)
                        {
                            string value = instanceParameter == null ? Smv.InitialValue(variable) : _clearZeroValues(instanceParameter.Value, variable.SmvType.GetType());
                            buffersInit += String.Format(Smv.VarInitializationBlock, instance.Name + "_" + variable.Name, value);
                        }
                        else
                        {
                            string[] values;

                            if (instanceParameter == null)
                            {
                                values = new string[variable.ArraySize];
                                for (int i = 0; i < variable.ArraySize; i++) values[i] = Smv.InitialValue(variable);
                            }
                            else
                            {
                                char[] trimChars = { '[', ']' };
                                values = instanceParameter.Value.Trim(trimChars).Split(',');
                                if (values.Count() != variable.ArraySize) throw new Exception("Invalid array value " + instanceParameter.Value);
                            }
                            for (int i = 0; i < variable.ArraySize; i++)
                            {
                                buffersInit += String.Format(Smv.VarInitializationBlock, instance.Name + "_" + variable.Name + Smv.ArrayIndex(i), _clearZeroValues(values[i], variable.SmvType.GetType()));
                            }
                        }
                    }
                    buffersInit += String.Format(Smv.VarInitializationBlock, instance.Name + "_" + Smv.Alpha, Smv.False);
                    buffersInit += String.Format(Smv.VarInitializationBlock, instance.Name + "_" + Smv.Beta, Smv.False);
                    buffersInit += "\n";
                }
                return buffersInit;
            }
            public static string InternalEventConnections(IEnumerable<Connection> internalBuffers, bool useProcesses)
            {
                string eventConnections = "-- _internalEventConnections\n";
                MultiMap<string> eventConnectionsMap = _getEventConnectionsMap(internalBuffers);
                List<string> definesList = new List<string>();
                foreach (string dst in eventConnectionsMap.Keys)
                {

                    bool dstComponent;
                    string dstSmvVar = Smv.ConvertConnectionVariableName(dst, Smv.ModuleParameters.Event, out dstComponent);

                    string srcString = "\t("; //+ srcSmvVar + " : " + Smv.True + ";\n";
                    foreach (string src in eventConnectionsMap[dst])
                    {
                        bool srcComponent;
                        string srcSmvVar = Smv.ConvertConnectionVariableName(src, Smv.ModuleParameters.Event, out srcComponent);
                        if (srcComponent) srcString += srcSmvVar + " | ";
                        else srcString += String.Format("({0} & {1}) | ", srcSmvVar, Smv.Alpha);
                    }

                    srcString = srcString.TrimEnd(Smv.OrTrimChars) + ")";
                    if (useProcesses)
                    {
                        srcString += " : " + Smv.True + ";\n";
                        eventConnections += String.Format(Smv.NextCaseBlock + "\n", dstSmvVar, srcString);
                    }
                    else
                    {
                        string reset_string;
                        if (dstComponent)
                        {
                            string[] connectionDst = Smv.SplitConnectionVariableName(dst);
                            reset_string = String.Format("\t({0}.{1}_reset) : {2};\n", connectionDst[0], Smv.ModuleParameters.Event(connectionDst[1]), Smv.False);
                            srcString += " : " + Smv.True + ";\n";
                            eventConnections += String.Format(Smv.NextCaseBlock + "\n", dstSmvVar, srcString + reset_string);
                        }
                        else
                        {
                            //reset_string = String.Format("\t{0} : {1};\n", Smv.True, Smv.False);
                            //srcString = srcString.TrimEnd(Smv.OrTrimChars) + ") : " + Smv.True + ";\n";
                            //eventConnections += String.Format(Smv.EmptyNextCaseBlock + "\n", dstSmvVar, srcString + reset_string);
                            dstSmvVar += "_set";
                            definesList.Add(String.Format(Smv.DefineBlock, dstSmvVar, srcString));
                        }
                        
                    }

                }

                foreach (string def in definesList)
                {
                    eventConnections += def;
                }
                return eventConnections;
            }
            public static string InternalDataConnections(IEnumerable<Connection> internalBuffers, IEnumerable<WithConnection> withConnections, IEnumerable<Variable> allVariables, IEnumerable<FBInstance> instances)
            {
                string dataConnections = "-- _internalDataConnections\n";
                foreach (Connection connection in internalBuffers.Where(conn => conn.Type == ConnectionType.Data))
                {
                    bool srcComponent;
                    bool dstComponent;
                    string dstSmvVar = Smv.ConvertConnectionVariableName(connection.Destination, Smv.ModuleParameters.Variable, out dstComponent);
                    string srcSmvVar = Smv.ConvertConnectionVariableName(connection.Source, Smv.ModuleParameters.Variable, out srcComponent);
                    string srcString = "";
                    /*if (srcComponent && dstComponent)
                    {
                        //srcString = "\t" + srcSmvVar + " : " + Smv.True + ";\n"; //TODO: make direct connections without double-buffering
                        var srcVar = _findVariable(connection.Source, allVariables, instances);

                        if (srcVar.ArraySize == 0)
                            dataConnections += String.Format(Smv.NextVarAssignment + "\n", dstSmvVar, srcSmvVar);
                        else
                        {
                            for (int i = 0; i < srcVar.ArraySize; i++)
                            {
                                dataConnections += String.Format(Smv.NextVarAssignment + "\n", dstSmvVar + Smv.ArrayIndex(i), srcSmvVar + Smv.ArrayIndex(i));
                            }
                        }
                    }
                    else */
                    if (!srcComponent && dstComponent)
                    {
                        var dstVar = _findVariable(connection.Destination, allVariables, instances);
                        if (dstVar.ArraySize == 0)
                        {
                            srcString = FbSmvCommon.VarSamplingRule(connection.Source, withConnections, false);
                            dataConnections += String.Format(Smv.NextCaseBlock + "\n", dstSmvVar, srcString);
                        }
                        else
                        {
                            for (int i = 0; i < dstVar.ArraySize; i++)
                            {
                                srcString = FbSmvCommon.VarSamplingRule(connection.Source, withConnections, false, i);
                                dataConnections += String.Format(Smv.NextCaseBlock + "\n", dstSmvVar + Smv.ArrayIndex(i), srcString);
                            }
                        }
                    }
                    else if (srcComponent && !dstComponent)
                    {
                        IEnumerable<string> samplingEvents = _getSamplingEventNamesForVariable(connection.Destination, withConnections);
                        MultiMap<string> eventConnectionsMap = _getEventConnectionsMap(internalBuffers);

                        string eventSeed = "";
                        foreach (string ev in samplingEvents)
                        {
                            string src = "";
                            foreach (string parentEvent in eventConnectionsMap[ev])
                            {
                                bool dontCare;
                                src += Smv.ConvertConnectionVariableName(parentEvent, Smv.ModuleParameters.Event, out dontCare) + Smv.Or;
                            }
                            eventSeed += String.Format("({0}){1}", src.TrimEnd(Smv.OrTrimChars), Smv.Or);
                        }

                        var srcVar = _findVariable(connection.Source, allVariables, instances);
                        if (srcVar.ArraySize == 0)
                        {
                            srcString = String.Format("\t{0} : {1};\n", eventSeed.TrimEnd(Smv.OrTrimChars), srcSmvVar);
                            dataConnections += String.Format(Smv.NextCaseBlock + "\n", dstSmvVar, srcString);
                        }
                        else
                        {
                            for (int i = 0; i < srcVar.ArraySize; i++)
                            {
                                srcString = String.Format("\t{0} : {1};\n", eventSeed.TrimEnd(Smv.OrTrimChars), srcSmvVar + Smv.ArrayIndex(i));
                                dataConnections += String.Format(Smv.NextCaseBlock + "\n", dstSmvVar + Smv.ArrayIndex(i), srcString);
                            }
                        }
                    }
                }
                return dataConnections;
            }

            private static Variable _findVariable(string connectionPoint, IEnumerable<Variable> allVariables, IEnumerable<FBInstance> instances)
            {
                string[] nameSplit = Smv.SplitConnectionVariableName(connectionPoint);
                FBInstance fbInst = instances.FirstOrDefault(inst => inst.Name == nameSplit[0]);
                Variable foundVar = allVariables.FirstOrDefault(v => v.FBType == fbInst.InstanceType && v.Name == nameSplit[1]);
                return foundVar;
            }

            public static string ComponentEventOutputs(IEnumerable<Connection> connections, bool useProcesses)
            {
                string outputsString = "-- ComponentEventOutputs\n";
                if (useProcesses)
                {
                    foreach (string output in _getInternalEventOutputStrings(connections))
                    {
                        outputsString += String.Format(Smv.NextVarAssignment, output, Smv.False);
                    }
                }
                else
                {
                    foreach (string output in _getInternalEventOutputs(connections))
                    {
                        string[] outpSplit = Smv.SplitConnectionVariableName(output);
                        bool alreadyChecked;
                        string varName = Smv.ConvertConnectionVariableName(output, Smv.ModuleParameters.Event, out alreadyChecked);

                        string setRule = String.Format("\t{0}.{1}_set : {2};\n", outpSplit[0], Smv.ModuleParameters.Event(outpSplit[1]), Smv.True);
                        string resetRule = String.Format("\t{0} : {1};\n", Smv.True, Smv.False);
                        outputsString += String.Format(Smv.EmptyNextCaseBlock, varName, setRule + resetRule);
                    }
                }
                return outputsString;
            }
            public static string DefineOmega(IEnumerable<Connection> internalBuffers)
            {
                string omega = "";
                foreach (string output in _getInternalEventOutputStrings(internalBuffers))
                {
                    omega += Smv.Not + output + Smv.Or;
                }
                return String.Format(Smv.DefineBlock, Smv.Omega, omega.TrimEnd(Smv.OrTrimChars));
            }
            public static string InputEventsResetRules(IEnumerable<Event> events, bool useProcesses)
            {
                string rules = "";
                foreach (Event ev in events.Where(ev => ev.Direction == Direction.Input))
                {
                    if (useProcesses)
                    {
                        string resetRule = "\t" + Smv.Alpha + " : " + Smv.False + ";\n";
                        rules += String.Format(Smv.NextCaseBlock, Smv.ModuleParameters.Event(ev.Name), resetRule);
                    }
                    else
                    {
                        rules += String.Format(Smv.DefineBlock, Smv.ModuleParameters.Event(ev.Name) + "_reset", Smv.Alpha);
                    }
                }
                return rules;
            }

            private static MultiMap<string> _getEventConnectionsMap(IEnumerable<Connection> internalBuffers)
            {
                MultiMap<string> eventConnectionsMap = new MultiMap<string>();
                foreach (Connection connection in internalBuffers.Where(conn => conn.Type == ConnectionType.Event))
                {
                    eventConnectionsMap.Add(connection.Destination, connection.Source);
                }
                return eventConnectionsMap;
            }
            private static IEnumerable<string> _getSamplingEventNamesForVariable(string varName, IEnumerable<WithConnection> withConnections)
            {
                List<string> output = new List<string>();
                foreach (WithConnection conn in withConnections.Where(c => c.Var == varName))
                {
                    output.Add(conn.Event);
                }
                return output;
            }
            private static IEnumerable<string> _getInternalEventOutputStrings(IEnumerable<Connection> internalBuffers)
            {
                HashSet<string> internalEventOutputs = new HashSet<string>();
                foreach (Connection connection in internalBuffers.Where(conn => conn.Type == ConnectionType.Event))
                {
                    bool srcComponent;
                    string srcSmvVar = Smv.ConvertConnectionVariableName(connection.Source, Smv.ModuleParameters.Event, out srcComponent);
                    if (!internalEventOutputs.Contains(srcSmvVar))
                    {
                        if (srcComponent) internalEventOutputs.Add(srcSmvVar);
                    }
                }
                return internalEventOutputs;
            }

            private static IEnumerable<string> _getInternalEventOutputs(IEnumerable<Connection> internalBuffers)
            {
                HashSet<string> internalEventOutputs = new HashSet<string>();
                foreach (Connection connection in internalBuffers.Where(conn => conn.Type == ConnectionType.Event))
                {
                    bool srcComponent;
                    Smv.ConvertConnectionVariableName(connection.Source, Smv.ModuleParameters.Event, out srcComponent);
                    if (!internalEventOutputs.Contains(connection.Source))
                    {
                        if (srcComponent) internalEventOutputs.Add(connection.Source);
                    }
                }
                return internalEventOutputs;
            }
        }

    }
}