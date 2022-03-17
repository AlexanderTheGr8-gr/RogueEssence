﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace RogueEssence.Dev
{
    public static class ReflectionExt
    {
        public delegate string TypeStringConv(object member);

        public static T SerializeCopy<T>(T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(stream, obj);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

                stream.Flush();
                stream.Position = 0;

#pragma warning disable SYSLIB0011 // Type or member is obsolete
                return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }

        public static object CreateMinimalInstance(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (ctors.Length > 0)
            {
                List<Tuple<ConstructorInfo, int>> sortedctors = new List<Tuple<ConstructorInfo, int>>();
                foreach (ConstructorInfo info in ctors)
                    sortedctors.Add(new Tuple<ConstructorInfo, int>(info, info.GetParameters().Length));
                sortedctors.Sort(new Comparison<Tuple<ConstructorInfo, int>>((x, y) => x.Item2 - y.Item2));
                ParameterInfo[] parameters = sortedctors[0].Item1.GetParameters();
                //make defaults of arrays an empty array, just to be easy
                object[] usedParams = new object[parameters.Length];
                for (int ii = 0; ii < parameters.Length; ii++)
                {
                    if (parameters[ii].ParameterType.IsArray)
                        usedParams[ii] = Array.CreateInstance(parameters[ii].ParameterType.GetElementType(), 0);
                }
                return sortedctors[0].Item1.Invoke(usedParams);
            }
            return null;
        }

        //TODO: utilize flag
        public static object[] GetPassableAttributes(int flag, object[] attributes)
        {
            List<object> objects = new List<object>();
            foreach (object obj in attributes)
            {
                PassableAttribute att = obj as PassableAttribute;
                if (att != null && att.PassableArgFlag == flag)
                    objects.Add(att);
            }
            return objects.ToArray();
        }

        public static T FindAttribute<T>(object[] attributes) where T : Attribute
        {
            foreach (object obj in attributes)
            {
                T att = obj as T;
                if (att != null)
                    return att;
            }
            return null;
        }

        public static List<MemberInfo> GetSerializableMembers(this Type type)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            Type privateType = type;
            Dictionary<string, FieldInfo> backingFields = new Dictionary<string, FieldInfo>();
            while (privateType != null)
            {
                FieldInfo[] fields = privateType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                    backingFields[field.Name] = field;
                privateType = privateType.BaseType;
            }

            PropertyInfo[] myProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo info in myProperties)
            {
                List<string> keys = new List<string>();
                foreach (string key in backingFields.Keys)
                    keys.Add(key);
                for (int ii = 0; ii < keys.Count; ii++)
                {
                    if (backingFields[keys[ii]].Name == "<" + info.Name + ">k__BackingField")
                    {
                        members.Add(info);
                        backingFields.Remove(keys[ii]);
                        break;
                    }
                }
            }

            foreach (FieldInfo info in backingFields.Values)
                members.Add(info);

            FieldInfo[] myFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in myFields)
                members.Add(info);

            for (int ii = members.Count-1; ii >= 0; ii--)
            {
                if (members[ii].GetCustomAttributes(typeof(NonSerializedAttribute), false).Length > 0)
                    members.RemoveAt(ii);
            }

            return members;
        }

        public static List<MemberInfo> GetEditableMembers(this Type type)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            Type privateType = type;
            List<FieldInfo> backingFields = new List<FieldInfo>();
            while (privateType != null)
            {
                FieldInfo[] fields = privateType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                backingFields.AddRange(fields);
                privateType = privateType.BaseType;
            }
            PropertyInfo[] myProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo info in myProperties)
            {
                foreach (FieldInfo field in backingFields)
                {
                    if (field.Name == "<" + info.Name + ">k__BackingField")
                    {
                        members.Add(info);
                        break;
                    }
                }
            }

            FieldInfo[] myFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in myFields)
                members.Add(info);

            return members;
        }

        public static List<MemberInfo> GetFieldsAndProperties(this Type type, BindingFlags flags)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            PropertyInfo[] myProperties = type.GetProperties(flags);
            foreach (PropertyInfo info in myProperties)
            {
                if (info.GetGetMethod(false) != null && info.GetSetMethod(false) != null)
                    members.Add(info);
            }

            FieldInfo[] myFields = type.GetFields(flags);
            foreach (FieldInfo info in myFields)
                members.Add(info);

            return members;
        }


        // some logic borrowed from James Newton-King, http://www.newtonsoft.com
        public static void SetValue(this MemberInfo member, object property, object value)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = (PropertyInfo)member;
                //find the backing field
                List<MemberInfo> members = new List<MemberInfo>();
                Type privateType = member.DeclaringType;
                List<FieldInfo> backingFields = new List<FieldInfo>();
                while (privateType != null)
                {
                    FieldInfo[] fields = privateType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                    backingFields.AddRange(fields);
                    privateType = privateType.BaseType;
                }
                foreach (FieldInfo field in backingFields)
                {
                    if (field.Name == "<" + propertyInfo.Name + ">k__BackingField")
                    {
                        field.SetValue(property, value);
                        break;
                    }
                }
            }
            else if (member.MemberType == MemberTypes.Field)
                ((FieldInfo)member).SetValue(property, value);
            else
                throw new Exception("Property must be of type FieldInfo or PropertyInfo");
        }

        public static object GetValue(this MemberInfo member, object property)
        {
            if (member.MemberType == MemberTypes.Property)
                return ((PropertyInfo)member).GetValue(property, null);
            else if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).GetValue(property);
            else
                throw new Exception("Property must be of type FieldInfo or PropertyInfo");
        }

        public static Type GetMemberInfoType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", "member");
            }
        }


        public static string GetDisplayName(this Type type)
        {
            if (type.IsConstructedGenericType)
            {
                Type[] args = type.GetGenericArguments();
                StringBuilder str = new StringBuilder(type.Name);
                str.Append("<");
                for (int ii = 0; ii < args.Length; ii++)
                {
                    if (ii > 0)
                        str.Append(",");
                    str.Append(args[ii].GetDisplayName());
                }
                str.Append(">");
                return str.ToString();
            }
            else
                return type.Name;
        }

        public static List<Assembly> GetDependentAssemblies(params Assembly[] assemblies)
        {
            List<int> startIndices = new List<int>();
            Assembly[] candAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<int>[] assemblyToDependents = new List<int>[candAssemblies.Length];
            Dictionary<string, int> assemblyNameLookup = new Dictionary<string, int>();
            for (int ii = 0; ii < candAssemblies.Length; ii++)
            {
                //TODO: we don't use full names here because of version disagreements; is there a better way?
                assemblyNameLookup[candAssemblies[ii].GetName().Name] = ii;
                if (assemblies.Contains(candAssemblies[ii]))
                    startIndices.Add(ii);
                assemblyToDependents[ii] = new List<int>();
            }
            for (int ii = 0; ii < candAssemblies.Length; ii++)
            {
                AssemblyName[] names = candAssemblies[ii].GetReferencedAssemblies();
                for (int jj = 0; jj < names.Length; jj++)
                {
                    int nn;
                    if (assemblyNameLookup.TryGetValue(names[jj].Name, out nn))
                        assemblyToDependents[nn].Add(ii);
                }
            }
            List<Assembly> resultAssemblies = new List<Assembly>();
            bool[] traversed = new bool[candAssemblies.Length];
            foreach(int startIndex in startIndices)
                traverseDependentAssemblies(candAssemblies, traversed, assemblyToDependents, assemblyNameLookup, resultAssemblies, startIndex);
            return resultAssemblies;
        }

        private static void traverseDependentAssemblies(Assembly[] candAssemblies, bool[] traversed, List<int>[] assemblyToDependents,
            Dictionary<string, int> assemblyNameLookup, List<Assembly> resultAssemblies, int startIndex)
        {
            if (traversed[startIndex])
                return;
            resultAssemblies.Add(candAssemblies[startIndex]);
            traversed[startIndex] = true;
            foreach (int dependent in assemblyToDependents[startIndex])
                traverseDependentAssemblies(candAssemblies, traversed, assemblyToDependents, assemblyNameLookup, resultAssemblies, dependent);
        }

        /// <summary>
        /// Use case: Child type inherits from generic parent type.  What generic argument did it use for the parent?
        /// This method finds the answer.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Type GetBaseTypeArg(Type parent, Type child, int index)
        {
            Type genericWithArgs = getAssignableFromGeneric(parent, child);
            return genericWithArgs.GetGenericArguments()[index];
        }

        public static Type[] GetAssignableTypes(this Type type)
        {
            if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                Type[] assignableArrays = arrayType.GetAssignableTypes();
                for (int ii = 0; ii < assignableArrays.Length; ii++)
                    assignableArrays[ii] = assignableArrays[ii].MakeArrayType();
                return assignableArrays;
            }
            else
            {
                List<Assembly> dependentAssemblies = GetDependentAssemblies(type.Assembly);
                List<Type> types = getAssignableTypes(false, 3, dependentAssemblies.ToArray(), type);
                types.Sort((Type type1, Type type2) => { return String.CompareOrdinal(type1.Name + type1.FullName, type2.Name + type2.FullName); });
                return types.ToArray();
            }
        }

        private static List<Type> getAssignableTypes(bool allowAbstract, int recursionDepth, Assembly[] searchAssemblies, params Type[] constraints)
        {
            if (recursionDepth == 0)
                return new List<Type>();

            

            List<Type> children = new List<Type>();
            foreach (Assembly assembly in searchAssemblies)
            {
                Type[] types = assembly.GetTypes();
                for (int ii = 0; ii < types.Length; ii++)
                {
                    Type checkType = types[ii];
                    //non-public classes cannot be used
                    if (!checkType.IsPublic)
                        continue;
                    //check if assignable; must be assignable to all
                    if (!allowAbstract && checkType.IsAbstract)
                        continue;

                    Type[] tracebackType = new Type[constraints.Length];
                    for(int jj = 0; jj < tracebackType.Length; jj++)
                    {
                        if (!constraints[jj].IsGenericType || !checkType.IsGenericType)
                            tracebackType[jj] = constraints[jj].IsAssignableFrom(checkType) ? constraints[jj] : null;
                        else //only if they both contain generic parameters will we have to invoke this
                            tracebackType[jj] = getAssignableFromGeneric(constraints[jj], checkType);

                        if (tracebackType[jj] == null)
                        {
                            tracebackType = null;
                            break;
                        }
                    }

                    if (tracebackType == null)
                        continue;

                    if (!checkType.ContainsGenericParameters)
                        children.Add(checkType);
                    else
                    {
                        //checkType is now currently a generic type whos argument-less form inherits from the constraint type's argument-less form
                        //now, in order to see if the generic type itself has a filled form that can satisfy the constraint,
                        //we must first fill all argument-less parameters with the existing arguments of the constraint
                        //as well as check to see if the arguments that ARE filled are filled with the right types

                        //checkType contains unassigned generic parameters
                        //constraints may have assigned generic parameters
                        //if they do, tracebackType maps back to them
                        Type[] checkArgs = checkType.GetGenericArguments();
                        Type[] paramsFromBase = new Type[checkArgs.Length];
                        bool correctFill = true;
                        for (int jj = 0; jj < tracebackType.Length; jj++)
                        {
                            Type[] filledArgs = constraints[jj].GetGenericArguments();
                            Type[] tracebackArgs = tracebackType[jj].GetGenericArguments();

                            for (int kk = 0; kk < tracebackArgs.Length; kk++)
                            {
                                if (tracebackArgs[kk].IsGenericParameter && paramsFromBase[tracebackArgs[kk].GenericParameterPosition] == null)
                                    paramsFromBase[tracebackArgs[kk].GenericParameterPosition] = filledArgs[kk];
                                else if (tracebackArgs[kk] != filledArgs[kk])
                                    correctFill = false;
                            }
                        }
                        if (correctFill)
                        {
                            Type[] chosenParams = new Type[checkArgs.Length];

                            //TODO: place proclaimed types beforehand,
                            //and let recursion bump into those,
                            //instead of the other way around?

                            Stack<Type[]> pendingParams = new Stack<Type[]>();
                            pendingParams.Push(paramsFromBase);
                            Stack<int> typeIndex = new Stack<int>();
                            typeIndex.Push(0);
                            defSpecifiedGenericParameter(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, new Stack<Type[]>(), new Stack<int>());
                        }
                    }
                }
            }

            return children;
        }

        private static void defSpecifiedGenericParameter(List<Type> children, int recursionDepth, Assembly[] searchAssemblies, Type checkType, Type[] checkArgs, Type[] chosenParams, Stack<Type[]> pendingParams, Stack<int> typeIndex, Stack<Type[]> constraints, Stack<int> constraintIndex)
        {
            int origIndex = typeIndex.Peek();
            while (typeIndex.Peek() < checkArgs.Length && pendingParams.Peek()[typeIndex.Peek()] == null)
                typeIndex.Push(typeIndex.Pop() + 1);

            if (typeIndex.Peek() == checkArgs.Length)
            {
                //all types have been filled for this recursion depth
                Type[] baseParams = pendingParams.Pop();
                int prevIndex = typeIndex.Pop();

                if (typeIndex.Count == 0)
                {
                    //base case.  all types from base have been filled in.
                    //now, decide on the next wave of unspecified type params
                    int openTypes = chosenParams.Length;
                    for (int ii = 0; ii < chosenParams.Length; ii++)
                    {
                        if (chosenParams[ii] != null)
                            openTypes--;
                    }
                    recurseTypeParamConstraints(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, openTypes);
                }
                else
                {
                    //go on to checking the next constraint
                    constraintIndex.Push(constraintIndex.Pop() + 1);
                    testSpecifiedGenericParameterConstraint(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, constraints, constraintIndex);
                    constraintIndex.Push(constraintIndex.Pop() - 1);
                }

                typeIndex.Push(prevIndex);
                pendingParams.Push(baseParams);
            }
            else
            {
                //recursive case. 
                //attempt to def one generic parameter
                Type prevType = chosenParams[typeIndex.Peek()];
                //if this type has already been filled in, and is different from the enforced fill-in, then we have a conflict and cannot proceed.
                Type newType = pendingParams.Peek()[typeIndex.Peek()];
                if (prevType == null)
                {

                    chosenParams[typeIndex.Peek()] = newType;

                    Type[] newConstraints = checkArgs[typeIndex.Peek()].GetGenericParameterConstraints();
                    constraints.Push(newConstraints);
                    constraintIndex.Push(0);
                    testSpecifiedGenericParameterConstraint(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, constraints, constraintIndex);
                    constraintIndex.Pop();
                    constraints.Pop();

                    chosenParams[typeIndex.Peek()] = prevType;
                }
                else if (newType.Equals(prevType))
                {
                    //we've completed this one already; move on.
                    typeIndex.Push(typeIndex.Pop() + 1);
                    defSpecifiedGenericParameter(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, constraints, constraintIndex);
                    typeIndex.Push(typeIndex.Pop() - 1);
                }
            }
            typeIndex.Pop();
            typeIndex.Push(origIndex);
        }

        private static void testSpecifiedGenericParameterConstraint(List<Type> children, int recursionDepth, Assembly[] searchAssemblies, Type checkType, Type[] checkArgs, Type[] chosenParams, Stack<Type[]> pendingParams, Stack<int> typeIndex, Stack<Type[]> constraints, Stack<int> constraintIndex)
        {
            if (constraintIndex.Peek() == constraints.Peek().Length) //verified all constraints for this argument, move on to the next argument
            {
                //all constraints have been satisfied for this recursion depth
                Type[] oldConstraints = constraints.Pop();
                int prevIndex = constraintIndex.Pop();

                //go on to checking the next type
                typeIndex.Push(typeIndex.Pop() + 1);
                defSpecifiedGenericParameter(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, constraints, constraintIndex);
                typeIndex.Push(typeIndex.Pop() - 1);

                constraintIndex.Push(prevIndex);
                constraints.Push(oldConstraints);
            }
            else if (constraints.Peek()[constraintIndex.Peek()].ContainsGenericParameters)
            {
                //first, get the type assignments possible
                List<Type[]> satisfyingConstraints = new List<Type[]>();
                getAssignableFromDerivedToConstraint(satisfyingConstraints, pendingParams.Peek()[typeIndex.Peek()], constraints.Peek()[constraintIndex.Peek()], checkArgs.Length);

                foreach (Type[] assignmentGroup in satisfyingConstraints)
                {
                    //attempt each group of type assginment
                    pendingParams.Push(assignmentGroup);
                    typeIndex.Push(0);
                    defSpecifiedGenericParameter(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, constraints, constraintIndex);
                    typeIndex.Pop();
                    pendingParams.Pop();
                }

            }
            else if (constraints.Peek()[constraintIndex.Peek()].IsAssignableFrom(pendingParams.Peek()[typeIndex.Peek()]))
            {
                constraintIndex.Push(constraintIndex.Pop() + 1);
                testSpecifiedGenericParameterConstraint(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, pendingParams, typeIndex, constraints, constraintIndex);
                constraintIndex.Push(constraintIndex.Pop() - 1);
            }
            //otherwise, it's the end of the line; don't do anything.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="satisfyingConstraints"></param>
        /// <param name="derived">Does not contain generic parameters</param>
        /// <param name="constraint">May contain generic parameters</param>
        private static void getAssignableFromDerivedToConstraint(List<Type[]> satisfyingConstraints, Type derived, Type constraint, int argAmount)
        {
            if (constraint.IsGenericParameter)
            {
                //just use all interfaces/classes down to object
                Type[] interfaceTypes = derived.GetInterfaces();

                foreach (Type intOther in interfaceTypes)
                {
                    Type[] returnArray = new Type[argAmount];
                    recursiveFillPotentialConstraints(returnArray, intOther, constraint);
                    satisfyingConstraints.Add(returnArray);
                }

                Type baseOther = derived;
                while (baseOther != null)
                {
                    Type[] returnArray = new Type[argAmount];
                    recursiveFillPotentialConstraints(returnArray, baseOther, constraint);
                    satisfyingConstraints.Add(returnArray);
                }
            }
            else if (constraint.IsInterface)
            {
                Type[] interfaceTypes = derived.GetInterfaces();

                foreach (Type baseOther in interfaceTypes)
                {
                    if (baseOther.IsGenericType && baseOther.GetGenericTypeDefinition() == constraint.GetGenericTypeDefinition())
                    {
                        //if they match without arguments
                        //check to see if the argument list of baseOther would work on the constraint
                        //...but actually, it always will.  because baseOther has already been proven to work when it comes down to just checking against a base class/interface
                        Type[] returnArray = new Type[argAmount];
                        recursiveFillPotentialConstraints(returnArray, baseOther, constraint);
                        satisfyingConstraints.Add(returnArray);
                    }
                }
            }
            else
            {
                //go back up the inheritance chain
                Type baseOther = derived;
                while (baseOther != null)
                {
                    if (baseOther.IsGenericType && baseOther.GetGenericTypeDefinition() == constraint.GetGenericTypeDefinition())
                    {
                        Type[] returnArray = new Type[argAmount];
                        recursiveFillPotentialConstraints(returnArray, baseOther, constraint);
                        satisfyingConstraints.Add(returnArray);
                        return;
                    }

                    baseOther = baseOther.BaseType;

                }
            }
        }

        private static void recursiveFillPotentialConstraints(Type[] satisfyingConstraint, Type closedType, Type openType)
        {
            if (openType.IsGenericParameter)
                satisfyingConstraint[openType.GenericParameterPosition] = closedType;
            else if (openType.ContainsGenericParameters)
            {
                Type[] closedGenerics = closedType.GetGenericArguments();
                Type[] openGenerics = openType.GetGenericArguments();

                for (int ii = 0; ii < closedGenerics.Length; ii++)
                    recursiveFillPotentialConstraints(satisfyingConstraint, closedGenerics[ii], openGenerics[ii]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="genericType">A non-constructed generic type</param>
        /// <param name="genericOther">Any generic type</param>
        /// <returns></returns>
        private static Type getAssignableFromGeneric(Type parent, Type child)
        {
            Type genericType = parent.GetGenericTypeDefinition();

            if (genericType.IsInterface)
            {
                Type[] interfaceTypes = child.GetInterfaces();

                foreach (Type baseOther in interfaceTypes)
                {
                    if (baseOther.IsGenericType && baseOther.GetGenericTypeDefinition() == genericType)
                        return baseOther;
                }
                return null;
            }
            else
            {
                Type baseOther = child;
                while (baseOther != null)
                {
                    if (baseOther.IsGenericType && baseOther.GetGenericTypeDefinition() == genericType)
                        return baseOther;

                    baseOther = baseOther.BaseType;

                }
                return null;
            }
        }

        /// <summary>
        /// Takes a non-constructed generic type and gets all possible constructions via recursion.
        /// Constructed types are added to the children parameter.
        /// </summary>
        /// <param name="children">The full list of child classes to the original type used in GetAssignableTypes</param>
        /// <param name="recursionDepth"></param>
        /// <param name="searchAssemblies"></param>
        /// <param name="checkType"></param>
        /// <param name="checkArgs"></param>
        /// <param name="chosenParams"></param>
        /// <param name="openTypes"></param>
        private static void recurseTypeParamConstraints(List<Type> children, int recursionDepth, Assembly[] searchAssemblies, Type checkType, Type[] checkArgs, Type[] chosenParams, int openTypes)
        {
            if (openTypes == 0)
            {
                Type constructedType = checkType.MakeGenericType(chosenParams.ToArray());
                children.Add(constructedType);
                return;
            }

            // get all possible types for the first wave of generic type arguments:
            Type[][] possibleParams = new Type[checkArgs.Length][];
            // the ones that are not constrained by other generic type arguments or non-constructed generic types
            for (int jj = 0; jj < checkArgs.Length; jj++)
            {
                //skip the params already chosen
                if (chosenParams[jj] != null)
                    continue;
                Type[] tpConstraints = checkArgs[jj].GetGenericParameterConstraints();
                //skip if this param is being constrained by other unassigned generic type arguments, or non-constructed generic types
                if (hasOpenConstraint(chosenParams, tpConstraints))
                    continue;
                //if this param is constrained by other generic type arguments, they will have been assigned at this point
                //it must be reflected in the constraints
                fillUnassignedConstraints(chosenParams, tpConstraints);

                Assembly[] assemblies = new Assembly[tpConstraints.Length];
                for (int kk = 0; kk < assemblies.Length; kk++)
                    assemblies[kk] = tpConstraints[kk].Assembly;
                List<Assembly> dependentAssemblies = GetDependentAssemblies(assemblies);
                possibleParams[jj] = getAssignableTypes(true, recursionDepth-1, dependentAssemblies.ToArray(), tpConstraints).ToArray();
            }

            recurseTypeParams(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, openTypes, possibleParams, 0);
        }

        /// <summary>
        /// Check to see if the constraints are unassigned generic type arguments, or non-constructed generic types
        /// </summary>
        /// <param name="chosenParams"></param>
        /// <param name="constraints"></param>
        /// <returns></returns>
        private static bool hasOpenConstraint(Type[] chosenParams, Type[] constraints)
        {
            foreach (Type constraint in constraints)
            {
                if (isOpenConstraint(chosenParams, constraint))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check to see if the constraint is an unassigned generic type argument, or a non-constructed generic type.
        /// </summary>
        /// <param name="chosenParams"></param>
        /// <param name="tConstraint"></param>
        /// <returns></returns>
        private static bool isOpenConstraint(Type[] chosenParams, Type tConstraint)
        {
            if (tConstraint.IsGenericParameter)
            {
                Type type = chosenParams[tConstraint.GenericParameterPosition];
                if (type == null)
                    return true;
                return type.ContainsGenericParameters;
            }
            else if (tConstraint.ContainsGenericParameters)
            {
                Type[] typeArguments = tConstraint.GetGenericArguments();

                foreach (Type tParam in typeArguments)
                {
                    if (isOpenConstraint(chosenParams, tParam))
                        return true;
                }
            }
            return false;
        }

        private static void fillUnassignedConstraints(Type[] chosenParams, Type[] constraints)
        {
            for (int ii = 0; ii < constraints.Length; ii++)
                constraints[ii] = getConstructedConstraint(chosenParams, constraints[ii]);
        }

        private static Type getConstructedConstraint(Type[] chosenParams, Type tConstraint)
        {
            if (tConstraint.IsGenericParameter)
                return chosenParams[tConstraint.GenericParameterPosition];
            else if (tConstraint.ContainsGenericParameters)
            {
                Type[] typeArguments = tConstraint.GetGenericArguments();
                for (int ii = 0; ii < typeArguments.Length; ii++)
                    typeArguments[ii] = getConstructedConstraint(chosenParams, typeArguments[ii]);

                //type argument filling only works for generic type definitions.
                //tConstraint, being a constraint, is not a generic type definition at start.
                //so it must be converted
                Type typeDefConstraint = tConstraint.GetGenericTypeDefinition();
                return typeDefConstraint.MakeGenericType(typeArguments);
            }
            return tConstraint;
        }

        /// <summary>
        /// Takes a jagged array of types that serve as candidates for the unconstructed generic type and adds all combinations to the children list.
        /// </summary>
        /// <param name="children"></param>
        /// <param name="recursionDepth"></param>
        /// <param name="searchAssemblies"></param>
        /// <param name="checkType">The uconstructed generic type</param>
        /// <param name="checkArgs"></param>
        /// <param name="chosenParams"></param>
        /// <param name="openTypes"></param>
        /// <param name="possibleParams">THe jagged array of types</param>
        /// <param name="paramIndex"></param>
        private static void recurseTypeParams(List<Type> children, int recursionDepth, Assembly[] searchAssemblies, Type checkType, Type[] checkArgs, Type[] chosenParams, int openTypes, Type[][] possibleParams, int paramIndex)
        {
            //skip the params that have been chosen already, and the possibleParam slots that haven't been filled
            while (paramIndex < checkArgs.Length && (chosenParams[paramIndex] != null || possibleParams[paramIndex] == null))
                paramIndex++;
            //base case.  All params have been chosen.  It's time to construct the type.
            //it is also possible that not all types have been chosen because the possibleParams had some null entries
            //those null entries are due to the argument at that position being dependent upon other params.
            if (paramIndex == checkArgs.Length)
            {
                recurseTypeParamConstraints(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, openTypes);
                return;
            }

            for (int ii = 0; ii < possibleParams[paramIndex].Length; ii++)
            {
                chosenParams[paramIndex] = possibleParams[paramIndex][ii];
                recurseTypeParams(children, recursionDepth, searchAssemblies, checkType, checkArgs, chosenParams, openTypes-1, possibleParams, paramIndex+1);
                chosenParams[paramIndex] = null;
            }
        }
    }
}
