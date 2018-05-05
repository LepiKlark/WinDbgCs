﻿using CsDebugScript.Engine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CsDebugScript.UI.ResultVisualizers
{
    /// <summary>
    /// Helper (base) class that for classes that want to implement <see cref="IResultVisualizer"/>.
    /// </summary>
    internal abstract class ResultVisualizer : IResultVisualizer
    {
        /// <summary>
        /// Number of array elements that should be visualized in a group.
        /// </summary>
        public const int ArrayElementsVisualized = 100;

        /// <summary>
        /// Resulting object that should be visualized.
        /// </summary>
        protected object result;

        /// <summary>
        /// Type of the resulting object that should be visualized.
        /// </summary>
        protected Type resultType;

        /// <summary>
        /// Interactive result visualizer that can be used for creating UI elements.
        /// </summary>
        protected InteractiveResultVisualizer interactiveResultVisualizer;

        /// <summary>
        /// Cache of the <see cref="Value"/> property.
        /// </summary>
        private SimpleCache<object> value;

        /// <summary>
        /// Cache of the <see cref="Type"/> property.
        /// </summary>
        private SimpleCache<string> typeString;

        /// <summary>
        /// Cache of the <see cref="ValueString"/> property.
        /// </summary>
        private SimpleCache<string> valueString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultVisualizer"/> class.
        /// </summary>
        /// <param name="result">Resulting object that should be visualized.</param>
        /// <param name="resultType">Type of the resulting object that should be visualized.</param>
        /// <param name="name">Name of the variable / property.</param>
        /// <param name="image">Image that represents icon of the variable / property</param>
        /// <param name="interactiveResultVisualizer">Interactive result visualizer that can be used for creating UI elements.</param>
        public ResultVisualizer(object result, Type resultType, string name, ImageSource image, InteractiveResultVisualizer interactiveResultVisualizer)
        {
            this.result = result;
            this.resultType = resultType;
            this.interactiveResultVisualizer = interactiveResultVisualizer;
            Name = name;
            Image = image;
            value = SimpleCache.Create(GetValue);
            typeString = SimpleCache.Create(GetTypeString);
            valueString = SimpleCache.Create(GetValueString);
        }

        /// <summary>
        /// Gets the name of the variable / property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the image that represents icon of the variable / property.
        /// </summary>
        public ImageSource Image { get; private set; }

        /// <summary>
        /// Gets the value of the property that will be visualized.
        /// If it is not <see cref="UIElement"/>, it will be added as a string (<see cref="ValueString"/>).
        /// </summary>
        public object Value => value.Value;

        /// <summary>
        /// Gets the string that represents type of the variable / property.
        /// </summary>
        public string Type => typeString.Value;

        /// <summary>
        /// Checks if this item has child elements and should be expandable.
        /// </summary>
        public abstract bool IsExpandable { get; }

        /// <summary>
        /// Gets the string that describes value of the variable / property.
        /// </summary>
        public string ValueString => valueString.Value;

        /// <summary>
        /// Gets the child elements in groups.
        /// </summary>
        public virtual IEnumerable<Tuple<string, IEnumerable<IResultVisualizer>>> Children
        {
            get
            {
                if (ExpandedChildren.Any())
                {
                    yield return Tuple.Create("[Expanded]", ExpandedChildren);
                }
                if (NonPublicChildren.Any())
                {
                    yield return Tuple.Create("[Internal]", OrderItems(NonPublicChildren));
                }
                if (StaticChildren.Any())
                {
                    yield return Tuple.Create("[Static]", OrderItems(StaticChildren));
                }
                if (DynamicChildren.Any())
                {
                    yield return Tuple.Create("[Dynamic]", OrderItems(DynamicChildren));
                }
                if (EnumerationChildren.Any())
                {
                    yield return Tuple.Create("[Enumeration]", OrderItems(EnumerationChildren));
                }
            }
        }

        /// <summary>
        /// Gets child elements that should be expanded when this object is visualized.
        /// This group will not be shown as a group like other properties like <see cref="DynamicChildren"/>.
        /// </summary>
        public virtual IEnumerable<IResultVisualizer> ExpandedChildren
        {
            get
            {
                return OrderItems(PublicChildren);
            }
        }

        /// <summary>
        /// Gets child elements that will be shown in [Public] group.
        /// It usualy represents public fields and properties.
        /// </summary>
        public virtual IEnumerable<IResultVisualizer> PublicChildren => Enumerable.Empty<IResultVisualizer>();

        /// <summary>
        /// Gets child elements that will be shown in [Internal] group.
        /// It usualy represents internal/properted/private fields and properties.
        /// </summary>
        public virtual IEnumerable<IResultVisualizer> NonPublicChildren => Enumerable.Empty<IResultVisualizer>();

        /// <summary>
        /// Gets child elements that will be shown in [Static] group.
        /// It usualy represents static fields and properties.
        /// </summary>
        public virtual IEnumerable<IResultVisualizer> StaticChildren => Enumerable.Empty<IResultVisualizer>();

        /// <summary>
        /// Gets child elements that will be shown in [Dynamic] group.
        /// It usualy represents dynamic fields and properties that can be accessed by using dynamic keyword.
        /// </summary>
        public virtual IEnumerable<IResultVisualizer> DynamicChildren => Enumerable.Empty<IResultVisualizer>();

        /// <summary>
        /// Gets child elements that will be shown in [Enumeration] group.
        /// It usualy represents list of items that are available in result if it is enumeration.
        /// </summary>
        public virtual IEnumerable<IResultVisualizer> EnumerationChildren => Enumerable.Empty<IResultVisualizer>();

        /// <summary>
        /// Gets the value of the property that will be visualized.
        /// If it is not <see cref="UIElement"/>, it will be added as a string (<see cref="ValueString"/>).
        /// </summary>
        protected abstract object GetValue();

        /// <summary>
        /// Gets the string that represents type of the variable / property.
        /// </summary>
        protected abstract string GetTypeString();

        /// <summary>
        /// Gets the string that describes value of the variable / property.
        /// </summary>
        protected virtual string GetValueString()
        {
            try
            {
                return Value.ToString();
            }
            catch
            {
                return "<Exception during evaluation>";
            }
        }

        /// <summary>
        /// Initializes caches so that properties can be safely queried in the UI (STA) thread.
        /// </summary>
        public void Initialize()
        {
            try
            {
                object value = Value;
            }
            catch
            {
            }

            try
            {
                if (!(Value is UIElement))
                {
                    string value = ValueString;
                }
            }
            catch
            {
            }

            try
            {
                string type = Type;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Creates <see cref="IResultVisualizer"/> for resulting object.
        /// </summary>
        /// <param name="result">Resulting object that should be visualized.</param>
        /// <param name="resultType">Type of the resulting object that should be visualized.</param>
        /// <param name="name">Name of the variable / property.</param>
        /// <param name="image">Image that represents icon of the variable / property</param>
        /// <param name="interactiveResultVisualizer">Interactive result visualizer that can be used for creating UI elements.</param>
        /// <returns>Instance of <see cref="IResultVisualizer"/> interface that can be used to visualize resulting object.</returns>
        public static IResultVisualizer Create(object result, Type resultType, string name, ImageSource image, InteractiveResultVisualizer interactiveResultVisualizer)
        {
            if (result != null)
            {
                if (result.GetType().IsArray)
                {
                    return new ArrayResultVisualizer((Array)result, resultType, name, image, interactiveResultVisualizer);
                }
                else if (typeof(IDictionary).IsAssignableFrom(result.GetType()))
                {
                    return new DictionaryResultVisualizer((IDictionary)result, resultType, name, image, interactiveResultVisualizer);
                }
                else if (result.GetType() == typeof(Variable))
                {
                    return new VariableResultVisualizer(((Variable)result).DowncastInterface(), resultType, name, image, interactiveResultVisualizer);
                }
                else if (result.GetType() == typeof(VariableCollection))
                {
                    return new VariableCollectionResultVisualizer((VariableCollection)result, resultType, name, image, interactiveResultVisualizer);
                }
            }
            return new ObjectResultVisualizer(result, resultType, name, image, interactiveResultVisualizer);
        }

        /// <summary>
        /// Orders the specified items in accending order.
        /// </summary>
        /// <param name="items">Items to be ordered.</param>
        /// <returns>Ordered items.</returns>
        protected static IEnumerable<IResultVisualizer> OrderItems(IEnumerable<IResultVisualizer> items)
        {
            return items
                .OrderBy(s =>
                {
                    if (s.Name.StartsWith("[") && s.Name.EndsWith("]"))
                    {
                        int value;

                        if (int.TryParse(s.Name.Substring(1, s.Name.Length - 2), out value))
                        {
                            return value;
                        }
                    }
                    return int.MaxValue;
                })
                .ThenBy(s => s.Name);
        }
    }
}
