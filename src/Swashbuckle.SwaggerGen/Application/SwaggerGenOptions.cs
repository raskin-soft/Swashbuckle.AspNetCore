﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;
using Swashbuckle.SwaggerGen.Annotations;

namespace Swashbuckle.SwaggerGen.Application
{
    public class SwaggerGenOptions
    {
        private readonly SwaggerGeneratorSettings _swaggerGeneratorSettings;
        private readonly SchemaRegistrySettings _schemaRegistrySettings;

        private List<FilterDescriptor<IOperationFilter>> _operationFilterDescriptors;
        private List<FilterDescriptor<IDocumentFilter>> _documentFilterDescriptors;
        private List<FilterDescriptor<ISchemaFilter>> _schemaFilterDescriptors;

        private struct FilterDescriptor<TFilter>
        {
            public Type Type;
            public object[] Arguments;
        }

        public SwaggerGenOptions()
        {
            _swaggerGeneratorSettings = new SwaggerGeneratorSettings();
            _schemaRegistrySettings = new SchemaRegistrySettings();

            _operationFilterDescriptors = new List<FilterDescriptor<IOperationFilter>>();
            _documentFilterDescriptors = new List<FilterDescriptor<IDocumentFilter>>();
            _schemaFilterDescriptors = new List<FilterDescriptor<ISchemaFilter>>();

            // Enable Annotations
            OperationFilter<SwaggerAttributesOperationFilter>();
            SchemaFilter<SwaggerAttributesSchemaFilter>();
        }

        public void SwaggerDoc(string name, Info info, Func<ApiDescription, bool> includeActionPredicate = null)
        {
            _swaggerGeneratorSettings.SwaggerDocs.Add(name, new SwaggerDocumentDescriptor(info, includeActionPredicate));
        }

        public void IgnoreObsoleteActions()
        {
            _swaggerGeneratorSettings.IgnoreObsoleteActions = true;
        }

        public void GroupActionsBy(Func<ApiDescription, string> groupNameSelector)
        {
            _swaggerGeneratorSettings.GroupNameSelector = groupNameSelector;
        }

        public void OrderActionGroupsBy(IComparer<string> groupNameComparer)
        {
            _swaggerGeneratorSettings.GroupNameComparer = groupNameComparer;
        }

        public void AddSecurityDefinition(string name, SecurityScheme securityScheme)
        {
            _swaggerGeneratorSettings.SecurityDefinitions.Add(name, securityScheme);
        }

        public void MapType(Type type, Func<Schema> schemaFactory)
        {
            _schemaRegistrySettings.CustomTypeMappings.Add(type, schemaFactory);
        }

        public void MapType<T>(Func<Schema> schemaFactory)
        {
            MapType(typeof(T), schemaFactory);
        }

        public void DescribeAllEnumsAsStrings()
        {
            _schemaRegistrySettings.DescribeAllEnumsAsStrings = true;
        }

        public void DescribeStringEnumsInCamelCase()
        {
            _schemaRegistrySettings.DescribeStringEnumsInCamelCase = true;
        }

        public void CustomSchemaIds(Func<Type, string> schemaIdSelector)
        {
            _schemaRegistrySettings.SchemaIdSelector = schemaIdSelector; 
        }

        public void IgnoreObsoleteProperties()
        {
            _schemaRegistrySettings.IgnoreObsoleteProperties = true;
        }

        public void OperationFilter<TFilter>(params object[] parameters)
            where TFilter : IOperationFilter
        {
            _operationFilterDescriptors.Add(new FilterDescriptor<IOperationFilter>
            {
                Type = typeof(TFilter),
                Arguments = parameters
            });
        }

        public void DocumentFilter<TFilter>(params object[] parameters)
            where TFilter : IDocumentFilter
        {
            _documentFilterDescriptors.Add(new FilterDescriptor<IDocumentFilter>
            {
                Type = typeof(TFilter),
                Arguments = parameters
            });
        }

        public void SchemaFilter<TFilter>(params object[] parameters)
            where TFilter : ISchemaFilter
        {
            _schemaFilterDescriptors.Add(new FilterDescriptor<ISchemaFilter>
            {
                Type = typeof(TFilter),
                Arguments = parameters
            });
        }

        public void IncludeXmlComments(string xmlDocPath)
        {
            var xmlDoc = new XPathDocument(xmlDocPath);
            OperationFilter<XmlCommentsOperationFilter>(xmlDoc);
            SchemaFilter<XmlCommentsSchemaFilter>(xmlDoc);
        }

        internal SchemaRegistrySettings GetSchemaRegistrySettings(IServiceProvider serviceProvider)
        {
            var settings = _schemaRegistrySettings.Clone();

            foreach (var filter in CreateFilters(_schemaFilterDescriptors, serviceProvider))
            {
                settings.SchemaFilters.Add(filter);
            }

            return settings;
        }

        internal SwaggerGeneratorSettings GetSwaggerGeneratorSettings(IServiceProvider serviceProvider)
        {
            var settings = _swaggerGeneratorSettings.Clone();

            foreach (var filter in CreateFilters(_operationFilterDescriptors, serviceProvider))
            {
                settings.OperationFilters.Add(filter);
            }

            foreach (var filter in CreateFilters(_documentFilterDescriptors, serviceProvider))
            {
                settings.DocumentFilters.Add(filter);
            }

            return settings;
        }

        private IEnumerable<TFilter> CreateFilters<TFilter>(
            List<FilterDescriptor<TFilter>> _filterDescriptors,
            IServiceProvider serviceProvider)
        {
            return _filterDescriptors.Select(descriptor =>
            {
                return (TFilter)ActivatorUtilities.CreateInstance(serviceProvider, descriptor.Type, descriptor.Arguments);
            });
        }
    }
}