namespace Nancy.ViewEngines.Razor
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using System.Web.Razor;
    using System.Web.Razor.Generator;
    using Microsoft.CSharp;
    using Microsoft.VisualBasic;
    using Nancy.Extensions;
    using Responses;

    public interface IRazorViewRenderer
    {
        IEnumerable<string> Assemblies { get; }

        string Extension { get; }

        RazorEngineHost Host { get; }

        CodeDomProvider Provider { get; }
    }
    
    public class VisualBasicRazorViewRenderer : IRazorViewRenderer
    {
        public VisualBasicRazorViewRenderer()
        {
            this.Assemblies = new List<string>();

            this.Provider = new VBCodeProvider();

            this.Host =
                new RazorEngineHost(new VBRazorCodeLanguage())
                {
                    DefaultBaseClass = typeof(NancyRazorViewBase).FullName,
                    DefaultNamespace = "RazorOutput",
                    DefaultClassName = "RazorView"
                };
        }

        public IEnumerable<string> Assemblies { get; private set; }

        public string Extension
        {
            get { return "vbhtml"; }
        }

        public RazorEngineHost Host { get; private set; }

        public CodeDomProvider Provider { get; private set; }
    }
    
    public class CSharpRazorViewRenderer : IRazorViewRenderer
    {
        public CSharpRazorViewRenderer()
        {
            this.Assemblies = new List<string>
            {
                typeof(Microsoft.CSharp.RuntimeBinder.Binder).GetAssemblyPath()
            };

            this.Provider = new CSharpCodeProvider();

            this.Host =
                new RazorEngineHost(new CSharpRazorCodeLanguage())
                {
                    DefaultBaseClass = typeof(NancyRazorViewBase).FullName,
                    DefaultNamespace = "RazorOutput",
                    DefaultClassName = "RazorView"
                };

            this.Host.NamespaceImports.Add("Microsoft.CSharp.RuntimeBinder");
        }

        public IEnumerable<string> Assemblies { get; private set; }

        public string Extension
        {
            get { return "cshtml"; }
        }

        public RazorEngineHost Host { get; private set; }

        public CodeDomProvider Provider { get; private set; }
    }

    /// <summary>
    /// View engine for rendering razor views.
    /// </summary>
    public class RazorViewEngine : IViewEngine
    {
        private readonly CodeDomProvider codeDomProvider;
        private readonly IRazorConfiguration razorConfiguration;
        private readonly IEnumerable<IRazorViewRenderer> viewRenderers;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine"/> class with a default configuration.
        /// </summary>
        public RazorViewEngine() : this(new DefaultRazorConfiguration())
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        public RazorViewEngine(IRazorConfiguration configuration)
        {
            this.viewRenderers = new List<IRazorViewRenderer>
            {
                new CSharpRazorViewRenderer(),
                new VisualBasicRazorViewRenderer()
            };

            this.razorConfiguration = configuration;
            this.codeDomProvider = new CSharpCodeProvider();
        }

        private RazorTemplateEngine GetRazorTemplateEngine(RazorEngineHost engineHost)
        {
            engineHost.GeneratedClassContext = 
                new GeneratedClassContext("Execute", "Write", "WriteLiteral", null, null, null, "DefineSection");

            engineHost.NamespaceImports.Add("System");
            engineHost.NamespaceImports.Add("System.IO");
            engineHost.NamespaceImports.Add("System.Web");

            if (this.razorConfiguration != null)
            {
                var namespaces = this.razorConfiguration.GetDefaultNamespaces();
                if (namespaces != null)
                {
                    foreach (var n in namespaces)
                    {
                        engineHost.NamespaceImports.Add(n);
                    }
                }
            }

            return new RazorTemplateEngine(engineHost);
        }

        private Func<NancyRazorViewBase> GetCompiledViewFactory<TModel>(string extension, TextReader reader, Assembly referencingAssembly)
        {
            var renderer = this.viewRenderers
                .Where(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                .First();

            var engine =
                this.GetRazorTemplateEngine(renderer.Host);

            var razorResult = 
                engine.GenerateCode(reader);

            var viewFactory = 
                this.GenerateRazorViewFactory(renderer.Provider, razorResult, referencingAssembly, renderer.Assemblies);

            return viewFactory;
        }

        private Func<NancyRazorViewBase> GenerateRazorViewFactory(CodeDomProvider codeProvider, GeneratorResults razorResult, Assembly referencingAssembly, IEnumerable<string> rendererSpecificAssemblies)
        {
            var outputAssemblyName =
                Path.Combine(Path.GetTempPath(), String.Format("Temp_{0}.dll", Guid.NewGuid().ToString("N")));

            var assemblies = new List<string>
            {
                GetAssemblyPath(typeof(System.Runtime.CompilerServices.CallSite)),
                GetAssemblyPath(typeof(IHtmlString)),
                GetAssemblyPath(Assembly.GetExecutingAssembly())
            };

            assemblies = assemblies
                .Union(rendererSpecificAssemblies)
                .ToList();

            if (this.razorConfiguration != null)
            {
                var assemblyNames = this.razorConfiguration.GetAssemblyNames();
                if (assemblyNames != null)
                {
                    assemblies.AddRange(assemblyNames.Select(Assembly.Load).Select(GetAssemblyPath));
                }
            }

            var loadReferencingAssembly = this.razorConfiguration == null || this.razorConfiguration.AutoIncludeModelNamespace;

            if (loadReferencingAssembly && referencingAssembly != null)
            {
                assemblies.Add(GetAssemblyPath(referencingAssembly));
            }

            var compilerParameters = 
                new CompilerParameters(assemblies.ToArray(), outputAssemblyName);

            var results = 
                codeProvider.CompileAssemblyFromDom(compilerParameters, razorResult.GeneratedCode);

            if (results.Errors.HasErrors)
            {
                var err = results.Errors
                    .OfType<CompilerError>()
                    .Where(ce => !ce.IsWarning)
                    .Select(error => String.Format("Error Compiling Template: ({0}, {1}) {2})", error.Line, error.Column, error.ErrorText))
                    .Aggregate((s1, s2) => s1 + "<br/>" + s2);

                return () => new NancyRazorErrorView(err);
            }
            
            var assembly = Assembly.LoadFrom(outputAssemblyName);
            if (assembly == null)
            {
                const string error = "Error loading template assembly";
                return () => new NancyRazorErrorView(error);
            }
            
            var type = assembly.GetType("RazorOutput.RazorView");
            if (type == null) 
            {
                var error = String.Format("Could not find type RazorOutput.Template in assembly {0}", assembly.FullName);
                return () => new NancyRazorErrorView(error);
            }

            if (Activator.CreateInstance(type) as NancyRazorViewBase == null)
            {
                const string error = "Could not construct RazorOutput.Template or it does not inherit from RazorViewBase";
                return () => new NancyRazorErrorView(error);
            }

            return () => (NancyRazorViewBase)Activator.CreateInstance(type);
        }

        private static string GetAssemblyPath(Type type) {
            return GetAssemblyPath(type.Assembly);
        }

        private static string GetAssemblyPath(Assembly assembly) {
            return new Uri(assembly.EscapedCodeBase).LocalPath;
        }

        private NancyRazorViewBase GetOrCompileView(ViewLocationResult viewLocationResult, IRenderContext renderContext, Assembly referencingAssembly)
        {
            var viewFactory = renderContext.ViewCache.GetOrAdd(
                viewLocationResult, 
                x => this.GetCompiledViewFactory<dynamic>(x.Extension, x.Contents.Invoke(), referencingAssembly));

            var view = viewFactory.Invoke();

            view.Code = string.Empty;

            return view;
        }

        /// <summary>
        /// Gets the extensions file extensions that are supported by the view engine.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> instance containing the extensions.</value>
        /// <remarks>The extensions should not have a leading dot in the name.</remarks>
        public IEnumerable<string> Extensions
        {
            get { return new[] { "cshtml", "vbhtml" }; }
        }

        public void Initialize(ViewEngineStartupContext viewEngineStartupContext)
        {
        }

        /// <summary>
        /// Renders the view.
        /// </summary>
        /// <param name="viewLocationResult">A <see cref="ViewLocationResult"/> instance, containing information on how to get the view template.</param>
        /// <param name="model">The model that should be passed into the view</param>
        /// <param name="renderContext"></param>
        /// <returns>A response.</returns>
        public Response RenderView(ViewLocationResult viewLocationResult, dynamic model, IRenderContext renderContext)
        {
            Assembly referencingAssembly = null;

            if (model != null)
            {
                var underlyingSystemType = model.GetType().UnderlyingSystemType;
                if (underlyingSystemType != null)
                {
                    referencingAssembly = Assembly.GetAssembly(underlyingSystemType);
                }
            }

            var response = new HtmlResponse();
            
            response.Contents = stream =>
                {
                    var writer =
                        new StreamWriter(stream);
                    NancyRazorViewBase view = this.GetViewInstance(viewLocationResult, renderContext, referencingAssembly, model);
                    view.ExecuteView(null, null);
                    var body = view.Body;
                    var sectionContents = view.SectionContents;
                    var root = !view.HasLayout;
                    var layout = view.Layout;
                
                    while (!root)
                    {
                        view = this.GetViewInstance(renderContext.LocateView(layout, model), renderContext, referencingAssembly, model);
                        view.ExecuteView(body, sectionContents);

                        body = view.Body;
                        sectionContents = view.SectionContents;
                        root = !view.HasLayout;
                    }

                    writer.Write(body);
                    writer.Flush();
                };

            return response;
        }

        private NancyRazorViewBase GetViewInstance(ViewLocationResult viewLocationResult, IRenderContext renderContext, Assembly referencingAssembly, dynamic model)
        {
            var view = this.GetOrCompileView(viewLocationResult, renderContext, referencingAssembly);
            view.Html = new HtmlHelpers(this, renderContext);
            view.Url = new UrlHelpers(this, renderContext);
            view.Model = model;
            return view;
        }
    }
}
