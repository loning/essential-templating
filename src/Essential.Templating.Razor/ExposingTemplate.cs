﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using Essential.Templating.Razor.Rendering;
using RazorEngine.Templating;

namespace Essential.Templating.Razor
{
    public class ExposingTemplate : Template, ITemplate, IExposingTemplate
    {
        private readonly object _syncRoot = new object();

        private ExecuteContextAdapter _executeContextAdapter;
        private ITemplateVisitor _templateVisitor;

        public ExposingTemplate(TemplateContext templateContext)
            : base(templateContext)
        {
            Contract.Requires<ArgumentNullException>(templateContext != null);
        }

        void IExposingTemplate.Run(ITemplateVisitor templateVisitor, object viewBag)
        {
            lock (_syncRoot)
            {
                _templateVisitor = templateVisitor;
                StringBuilder sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    ((ITemplate)this).Run(
                        new ExecuteContext(new ObjectViewBag(viewBag)),sw);
                    _templateVisitor.Body(sb.ToString());
                }
                _templateVisitor = null;
            }
        }


        void ITemplate.Run(ExecuteContext context, TextWriter writerTo)
        {
            var builder = new StringBuilder();
            _executeContextAdapter = new ExecuteContextAdapter(this, context);
            using (var writer = new StringWriter(builder))
            {
                _executeContextAdapter.CurrentWriter = writer;
                OnStart();
                Execute();
                OnEnd();
                _executeContextAdapter.CurrentWriter = null;
            }
            var parent = ResolveLayout(Layout);
            if (parent == null && string.IsNullOrEmpty(Layout))
            {
                writerTo.Write(builder.ToString());
                //return builder.ToString();
            }
            if (parent == null)
            {
                throw new InvalidOperationException("Layout template was not found.");
            }
            var exposingParent = parent as ExposingTemplate;
            if (exposingParent == null)
            {
                throw new InvalidOperationException("Unexpected layout template base type.");
            }
            exposingParent._templateVisitor = _templateVisitor;
            var bodyWriter = new TemplateWriter(tw => tw.Write(builder.ToString()));
            _executeContextAdapter.PushBody(bodyWriter);
            parent.Run(_executeContextAdapter.Context, writerTo);
        }

        //[DebuggerStepThrough]
        public override TemplateWriter RenderSection(string name, bool isRequired = true)
        {
            // Do not use debugger breakpoints in this code.
            if (_templateVisitor == null || !IsSectionDefined(name))
            {
                return base.RenderSection(name, isRequired);
            }
            var builder = ((StringWriter)_executeContextAdapter.CurrentWriter).GetStringBuilder();
            var start = builder.Length;
            var writer = base.RenderSection(name, isRequired);
            var ignore = writer.ToString();
            var end = builder.Length;
            var contentArray = new char[end - start];
            builder.CopyTo(start, contentArray, 0, end - start);
            var content = new string(contentArray);
            _templateVisitor.Section(name, content);
            return new TemplateWriter(w => { });
        }

        protected virtual void OnStart()
        {
            if (_templateVisitor != null)
            {
                _templateVisitor.Start(this);
            }
        }

        protected virtual void OnEnd()
        {
            if (_templateVisitor != null)
            {
                _templateVisitor.End(this);
            }
        }


    }
}