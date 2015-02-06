﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace Essential.Templating.Razor
{
    public class Template : TemplateBase
    {
        private readonly TemplateContext _templateContext;

        public Template(TemplateContext templateContext)
        {
            Contract.Requires<ArgumentNullException>(templateContext != null);

            _templateContext = templateContext;
        }

        public string Name
        {
            get { return _templateContext.Path; }
        }

        public CultureInfo Culture
        {
            get { return _templateContext.Culture; }
        }

        public override void WriteTo(TextWriter writer, object value)
        {
            if (value == null)
            {
                return;
            }
            var encodedString = value as IEncodedString;
            if (encodedString != null)
            {
                writer.Write(encodedString);
            }
            else
            {
                var factory = new HtmlEncodedStringFactory();
                writer.Write(factory.CreateEncodedString(value));
            }
        }

        public override TemplateWriter Include(string cacheName, object model = null, System.Type modelType = null)
        {
            var partial = _templateContext.RenderString(cacheName, Culture, model);
            return partial == null
                ? new TemplateWriter(w => { })
                : new TemplateWriter(w => w.Write(partial));
        }

        protected override ITemplate ResolveLayout(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var layout = _templateContext.Resolve(name, Culture);
            if (layout == null)
            {
                throw new InvalidOperationException("Layout template was not found.");
            }
            return layout;
        }

        protected ITemplate ResolveLayout(string name, Dictionary<string, object> contextEnvironment)
        {
            Contract.Requires(contextEnvironment != null);

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var layout = _templateContext.Resolve(name, Culture, contextEnvironment);
            if (layout == null)
            {
                throw new InvalidOperationException("Layout template was not found.");
            }
            return layout;
        }

        protected Stream GetResource(string uri, CultureInfo culture = null)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(uri));

            return _templateContext.ResourceProvider.Get(uri, culture);
        }

        protected TemplateContext DeriveContext(string path)
        {
            Contract.Requires(!string.IsNullOrEmpty(path));

            return _templateContext.Derive(path);
        }
    }
}