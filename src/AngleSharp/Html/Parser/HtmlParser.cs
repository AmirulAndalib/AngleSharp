﻿namespace AngleSharp.Html.Parser
{
    using AngleSharp.Dom;
    using AngleSharp.Dom.Events;
    using AngleSharp.Html.Dom;
    using AngleSharp.Text;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates an instance of the HTML parser front-end.
    /// </summary>
    public class HtmlParser : EventTarget, IHtmlParser
    {
        #region Fields

        private readonly HtmlParserOptions _options;
        private readonly IBrowsingContext _context;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the HTML parser is starting.
        /// </summary>
        public event DomEventHandler Parsing
        {
            add { AddEventListener(EventNames.ParseStart, value); }
            remove { RemoveEventListener(EventNames.ParseStart, value); }
        }

        /// <summary>
        /// Fired when the HTML parser is finished.
        /// </summary>
        public event DomEventHandler Parsed
        {
            add { AddEventListener(EventNames.ParseEnd, value); }
            remove { RemoveEventListener(EventNames.ParseEnd, value); }
        }

        /// <summary>
        /// Fired when a HTML parse error is encountered.
        /// </summary>
        public event DomEventHandler Error
        {
            add { AddEventListener(EventNames.ParseError, value); }
            remove { RemoveEventListener(EventNames.ParseError, value); }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Creates a new parser with the default options and context.
        /// </summary>
        public HtmlParser()
            : this(BrowsingContext.New())
        {
        }

        /// <summary>
        /// Creates a new parser with the custom options.
        /// </summary>
        /// <param name="options">The options to use.</param>
        public HtmlParser(HtmlParserOptions options)
            : this(options, BrowsingContext.New())
        {
        }

        /// <summary>
        /// Creates a new parser with the custom context.
        /// </summary>
        /// <param name="context">The context to use.</param>
        public HtmlParser(IBrowsingContext context)
            : this(new HtmlParserOptions { IsScripting = context.IsScripting() }, context)
        {
        }

        /// <summary>
        /// Creates a new parser with the custom options and the given context.
        /// </summary>
        /// <param name="options">The options to use.</param>
        /// <param name="context">The context to use.</param>
        public HtmlParser(HtmlParserOptions options, IBrowsingContext context)
        {
            _options = options;
            _context = context;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the specified options.
        /// </summary>
        public HtmlParserOptions Options
        {
            get { return _options; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the string and returns the result.
        /// </summary>
        public IHtmlDocument Parse(String source)
        {
            var document = CreateDocument(source);
            return Parse(document);
        }

        /// <summary>
        /// Parses the string and returns the result.
        /// </summary>
        public INodeList ParseFragment(String source, IElement contextElement)
        {
            var document = CreateDocument(source);
            var parser = new HtmlDomBuilder(document);
            var element = contextElement as Element;

            if (element != null)
            {
                var context = document.Context;
                var factory = context.GetFactory<IElementFactory<HtmlElement>>();
                element = factory.Create(document, contextElement.LocalName, contextElement.Prefix);
                return parser.ParseFragment(_options, element).DocumentElement.ChildNodes;
            }

            return parser.Parse(_options).ChildNodes;
        }

        /// <summary>
        /// Parses the stream and returns the result.
        /// </summary>
        public IHtmlDocument Parse(Stream source)
        {
            var document = CreateDocument(source);
            return Parse(document);
        }

        /// <summary>
        /// Parses the string asynchronously.
        /// </summary>
        public Task<IHtmlDocument> ParseAsync(String source)
        {
            return ParseAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Parses the stream asynchronously.
        /// </summary>
        public Task<IHtmlDocument> ParseAsync(Stream source)
        {
            return ParseAsync(source, CancellationToken.None);
        }

        /// <summary>
        /// Parses the string asynchronously with option to cancel.
        /// </summary>
        public Task<IHtmlDocument> ParseAsync(String source, CancellationToken cancel)
        {
            var document = CreateDocument(source);
            return ParseAsync(document, cancel);
        }

        /// <summary>
        /// Parses the stream asynchronously with option to cancel.
        /// </summary>
        public Task<IHtmlDocument> ParseAsync(Stream source, CancellationToken cancel)
        {
            var document = CreateDocument(source);
            return ParseAsync(document, cancel);
        }

        async Task<IDocument> IHtmlParser.ParseAsync(IDocument document, CancellationToken cancel)
        {
            var parser = CreateBuilder((HtmlDocument)document);
            InvokeEventListener(new HtmlParseEvent(document, completed: false));
            await parser.ParseAsync(_options, cancel).ConfigureAwait(false);
            InvokeEventListener(new HtmlParseEvent(document, completed: true));
            return document;
        }

        #endregion

        #region Helpers

        private HtmlDocument CreateDocument(String source)
        {
            var textSource = new TextSource(source);
            return CreateDocument(textSource);
        }

        private HtmlDocument CreateDocument(Stream source)
        {
            var encoding = _context.GetDefaultEncoding();
            var textSource = new TextSource(source, encoding);
            return CreateDocument(textSource);
        }

        private HtmlDocument CreateDocument(TextSource textSource)
        {
            var document = new HtmlDocument(_context, textSource);
            return document;
        }

        private HtmlDomBuilder CreateBuilder(HtmlDocument document)
        {
            var parser = new HtmlDomBuilder((HtmlDocument)document);

            if (HasEventListener(EventNames.ParseError))
            {
                parser.Error += (s, ev) => InvokeEventListener(ev);
            }

            return parser;
        }

        private IHtmlDocument Parse(HtmlDocument document)
        {
            var parser = CreateBuilder(document);
            InvokeEventListener(new HtmlParseEvent(document, completed: false));
            parser.Parse(_options);
            InvokeEventListener(new HtmlParseEvent(document, completed: true));
            return document;
        }

        private async Task<IHtmlDocument> ParseAsync(HtmlDocument document, CancellationToken cancel)
        {
            var parser = CreateBuilder(document);
            InvokeEventListener(new HtmlParseEvent(document, completed: false));
            await parser.ParseAsync(_options, cancel).ConfigureAwait(false);
            InvokeEventListener(new HtmlParseEvent(document, completed: true));
            return document;
        }

        #endregion
    }
}
