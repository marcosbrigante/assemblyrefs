using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AssemblyRefs
{
    /// <summary>
    /// Represents an assembly participating the analysis.
    /// </summary>
    public class AssemblyModel
    {
        #region Static Fields
        /// <summary>
        /// The assembly cache, which is loaded with all the models
        /// during the analysis.
        /// </summary>
        private static List<AssemblyModel> _assemblyModelCache = new List<AssemblyModel>();
        #endregion

        #region Static Properties
        /// <summary>
        /// Gets or sets the assembly model cache.
        /// </summary>
        /// <value>The assembly model cache.</value>
        public static List<AssemblyModel> AssemblyModelCache
        {
            get { return _assemblyModelCache; }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Analyzes the specified assembly target path.
        /// </summary>
        /// <param name="targetAssemblyPath">The target assembly path.</param>
        /// <returns>
        /// The target assembly references model.
        /// </returns>
        public static AssemblyModel Analyze(string targetAssemblyPath)
        {
            var assembly = new AssemblyModel();
            _assemblyModelCache.Add(assembly);
            assembly.LoadFirst(targetAssemblyPath);

            return assembly;
        }

        /// <summary>
        /// Gets the assembly model from the model cache.
        /// </summary>
        /// <param name="name">The desired assembly name.</param>
        /// <returns>
        /// The desired assembly reference  model.
        /// </returns>
        private static AssemblyModel GetAssemblyModel(string name)
        {
            if (_assemblyModelCache.Any(assembly => assembly.Name.Equals(name)))
            {
                return (_assemblyModelCache.First(assembly => assembly.Name.Equals(name)));
            }
            else
            {
                var currentAssembly = new AssemblyModel(name, false);
                _assemblyModelCache.Add(currentAssembly);
                currentAssembly.Load();
                return currentAssembly;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyModel"/> class.
        /// </summary>
        internal AssemblyModel()
        {
            References = new List<AssemblyModel>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyModel"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="found">if set to <c>true</c> [found].</param>
        internal AssemblyModel(string name, bool found)
        {
            Name = name;
            Found = found;
            References = new List<AssemblyModel>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the assembly name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AssemblyModel"/> is found.
        /// </summary>
        /// <value><c>true</c> if found; otherwise, <c>false</c>.</value>
        public bool Found { get; set; }

        /// <summary>
        /// Gets or sets the assembly references.
        /// </summary>
        /// <value>The references.</value>
        public List<AssemblyModel> References { get; set; }

        /// <summary>
        /// Gets or sets the type of the build.
        /// </summary>
        /// <value>The type of the build.</value>
        public AssemblyBuildType BuildType { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Loads this model instance as a target model in
        /// an analysis.
        /// </summary>
        /// <param name="targetAssemblyPath">The target assembly path.</param>
        public void LoadFirst(string targetAssemblyPath)
        {
            Assembly currentAssembly;
            currentAssembly = Assembly.LoadFrom(targetAssemblyPath);

            Name = currentAssembly.FullName;
            Found = true;
            BuildType = DiscoverBuildType(currentAssembly);

            AssemblyName[] referencesNames = currentAssembly.GetReferencedAssemblies();

            foreach (var referenceName in referencesNames)
            {
                var auxRef = GetAssemblyModel(referenceName.FullName);
                References.Add(auxRef);
            }
        }

        /// <summary>
        /// Loads this model instance as a reference model
        /// in an analysis.
        /// </summary>
        public void Load()
        {
            Assembly currentAssembly;

            try
            {
                currentAssembly = Assembly.Load(Name);
                BuildType = DiscoverBuildType(currentAssembly);
                Found = true;
            }
            catch
            {
                Found = false;
                BuildType = AssemblyBuildType.NotDefined;
                return;
            }

            AssemblyName[] referencesNames = currentAssembly.GetReferencedAssemblies();

            foreach (var referenceName in referencesNames)
            {
                var auxRef = GetAssemblyModel(referenceName.FullName);
                References.Add(auxRef);
            }
        }

        /// <summary>
        /// Discovers the type of the build of the
        /// given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>
        /// The corresponding item in the build type enumerator.
        /// </returns>
        private AssemblyBuildType DiscoverBuildType(Assembly assembly)
        {
            var isDebug = false;

            try
            {
                foreach (object att in assembly.GetCustomAttributes(false))
                    if (att is DebuggableAttribute)
                        isDebug = ((DebuggableAttribute)att).IsJITTrackingEnabled;

                return isDebug ? AssemblyBuildType.Debug : AssemblyBuildType.Release;
            }
            catch
            {
                return AssemblyBuildType.NotDefined;
            }


        }
        #endregion
    }

    /// <summary>
    /// The possible types of an assembly's build.
    /// </summary>
    public enum AssemblyBuildType
    {
        /// <summary>
        /// Build type could not be defined.
        /// </summary>
        NotDefined = 0,
        /// <summary>
        /// Debug build.
        /// </summary>
        Debug,
        /// <summary>
        /// Release build.
        /// </summary>
        Release
    }
}
