﻿using System;
using UnityEngine;
using System.CodeDom;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace BJSYGameCore.AutoCompo
{
    public class AutoCompoGenerator
    {
        /// <summary>
        /// 为游戏物体生成编译单元。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public CodeCompileUnit genScript4GO(GameObject gameObject, AutoCompoGenSetting setting)
        {
            _setting = setting;
            _rootGameObject = gameObject;
            CodeCompileUnit unit = new CodeCompileUnit();
            //命名空间，引用
            CodeNamespace nameSpace = new CodeNamespace(setting.Namespace);
            unit.Namespaces.Add(nameSpace);
            foreach (string import in setting.usings)
            {
                nameSpace.Imports.Add(new CodeNamespaceImport(import));
            }
            //类
            _type = new CodeTypeDeclaration();
            nameSpace.Types.Add(_type);
            _type.CustomAttributes.Add(new CodeAttributeDeclaration(nameof(AutoCompoAttribute),
                new CodeAttributeArgument(new CodePrimitiveExpression(gameObject.GetInstanceID()))));
            _type.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            _type.IsPartial = true;
            _type.IsClass = true;
            _type.Name = genTypeName4GO(gameObject);
            foreach (var baseType in setting.baseTypes)
            {
                _type.BaseTypes.Add(baseType);
            }
            //自动绑定方法
            _autoBindMethod = new CodeMemberMethod();
            _type.Members.Add(_autoBindMethod);
            _autoBindMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            _autoBindMethod.ReturnType = new CodeTypeReference("void");
            _autoBindMethod.Name = "autoBind";
            //根物体组件引用
            string[] compoTypes;
            if (tryParseGOName(gameObject.name, out _, out compoTypes))
            {
                foreach (var compoTypeName in compoTypes)
                {
                    Component component = gameObject.GetComponent(compoTypeName);
                    if (component == null)
                        continue;
                    genRootCompo(component);
                }
            }
            //处理子物体
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject childGO = gameObject.transform.GetChild(i).gameObject;
                genChildGO(childGO);
            }
            return unit;
        }

        void genRootCompo(Component component)
        {
            string fieldName = genFieldName4RootCompo(component);
            string[] path = new string[0];
            string propName = genPropName4RootCompo(component);
            genFieldPropInit4Compo(component, fieldName, propName, path);
        }

        void genFieldPropInit4Compo(Component component, string fieldName, string propName, string[] path)
        {
            genFieldWithInit4Compo(component, fieldName, path);
            genProp4Compo(component, propName, fieldName);
        }

        void genProp4Compo(Component component, string propName, string fieldName)
        {
            CodeMemberProperty prop = new CodeMemberProperty();
            _type.Members.Add(prop);
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            prop.Type = new CodeTypeReference(component.GetType().Name);
            prop.Name = propName;
            prop.HasGet = true;
            prop.GetStatements.Add(new CodeMethodReturnStatement(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                fieldName)));
        }
        void genFieldWithInit4Compo(Component component, string fieldName, string[] path)
        {
            genField4Compo(component, fieldName);
            CodeAssignStatement assign = new CodeAssignStatement();
            _autoBindMethod.Statements.Add(assign);
            assign.Left = new CodeFieldReferenceExpression(
                new CodeThisReferenceExpression(), fieldName);
            CodeExpression target = new CodeThisReferenceExpression();
            for (int i = 0; i < path.Length; i++)
            {
                if (i == 0)
                    target = new CodePropertyReferenceExpression(target, nameof(GameObject.transform));
                target = new CodeMethodInvokeExpression(target, nameof(Transform.Find),
                    new CodePrimitiveExpression(path[i]));
            }
            assign.Right = new CodeMethodInvokeExpression(
                target, nameof(GameObject.GetComponent),
                new CodePrimitiveExpression(component.GetType().Name));
        }
        void genField4Compo(Component component, string fieldName)
        {
            CodeMemberField field = new CodeMemberField();
            _type.Members.Add(field);
            foreach (var fieldAttName in _setting.fieldAttributes)
            {
                field.CustomAttributes.Add(new CodeAttributeDeclaration(fieldAttName));
            }
            field.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            field.Type = new CodeTypeReference(component.GetType().Name);
            field.Name = fieldName;
        }
        void genChildGO(GameObject gameObject)
        {
            string[] compoTypes;
            if (tryParseGOName(gameObject.name, out _, out compoTypes))
            {
                foreach (var compoTypeName in compoTypes)
                {
                    Component component = gameObject.GetComponent(compoTypeName);
                    if (component == null)
                        continue;
                    genFieldPropInit4Compo(component, genFieldName4Compo(component),
                        genPropName4Compo(component), getPath(_rootGameObject, gameObject));
                }
            }
            //处理子物体
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject childGO = gameObject.transform.GetChild(i).gameObject;
                genChildGO(childGO);
            }
        }
        string genTypeName4GO(GameObject gameObject)
        {
            if (tryParseGOName(gameObject.name, out var typeName, out _))
                return typeName;
            else
                throw new FormatException(gameObject.name + "不符合格式\\w.\\w*");
        }
        bool tryParseGOName(string name, out string typeName, out string[] compoTypes)
        {
            var match = Regex.Match(name, @"(?<name>.+)\.(?<args>\w+(,\w+)*)");
            if (match.Success)
            {
                typeName = match.Groups["name"].Value;
                compoTypes = match.Groups["args"].Value.Split(',');
                return true;
            }
            else
            {
                typeName = string.Empty;
                compoTypes = new string[0];
                return false;
            }
        }
        string genFieldName4RootCompo(Component component)
        {
            return "_as" + component.GetType().Name;
        }
        string genPropName4RootCompo(Component component)
        {
            return "as" + component.GetType().Name;
        }
        string genFieldName4Compo(Component component)
        {
            string fieldName;
            if (tryParseGOName(component.gameObject.name, out fieldName, out _))
            {
                return "_" + fieldName + component.GetType().Name;
            }
            else
                throw new FormatException();
        }
        string genPropName4Compo(Component component)
        {
            string propName;
            if (tryParseGOName(component.gameObject.name, out propName, out _))
                return propName + component.GetType().Name;
            else
                throw new FormatException();
        }
        string[] getPath(GameObject parent, GameObject child)
        {
            if (parent.transform == child.transform)
                return new string[0];
            List<string> pathList = new List<string>();
            for (Transform transform = child.transform; transform != null; transform = transform.parent)
            {
                if (transform.gameObject == parent)
                    break;
                pathList.Add(transform.gameObject.name);
            }
            return pathList.ToArray();
        }
        AutoCompoGenSetting _setting;
        GameObject _rootGameObject;
        CodeTypeDeclaration _type;
        CodeMemberMethod _autoBindMethod;
    }
    [Serializable]
    public class AutoCompoGenSetting
    {
        public string[] usings;
        public string Namespace;
        public string[] baseTypes;
        public string[] fieldAttributes;
    }
}