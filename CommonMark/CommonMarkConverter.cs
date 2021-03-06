﻿using CommonMark.Formatter;
using CommonMark.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CommonMark
{
    /// <summary>
    /// Contains methods for parsing and formatting CommonMark data.
    /// </summary>
    public static class CommonMarkConverter
    {
        /// <summary>
        /// Gets the CommonMark assembly version number. Note that might differ from the actual release version
        /// since the assembly version is not always incremented to reduce possible reference errors when updating.
        /// </summary>
        public static Version AssemblyVersion
        {
            get
            {
                var assembly = typeof(CommonMarkConverter).Assembly.FullName;
                var aName = new System.Reflection.AssemblyName(assembly);
                return aName.Version;
            }
        }

        /// <summary>
        /// Performs the first stage of the conversion - parses block elements from the source and created the syntax tree.
        /// </summary>
        /// <param name="source">The reader that contains the source data.</param>
        /// <param name="settings">The object containing settings for the parsing process.</param>
        /// <returns>The block element that represents the document.</returns>
        /// <exception cref="ArgumentNullException">when <paramref name="source"/> is <c>null</c></exception>
        /// <exception cref="CommonMarkException">when errors occur during block parsing.</exception>
        /// <exception cref="IOException">when error occur while reading the data.</exception>
        public static Syntax.Block ProcessStage1(TextReader source, CommonMarkSettings settings = null)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var cur = BlockMethods.make_document();

            int linenum = 1;
            try
            {
                var reader = new Parser.TabTextReader(source);
                while (!reader.EndOfStream())
                {
                    BlockMethods.incorporate_line(reader.ReadLine(), linenum, ref cur);
                    linenum++;
                }
            }
            catch(IOException)
            {
                throw;
            }
            catch(CommonMarkException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new CommonMarkException("An error occurred while parsing line " + linenum.ToString(CultureInfo.InvariantCulture), cur, ex);
            }

            try
            {
                while (cur != cur.Top)
                {
                    BlockMethods.finalize(cur, linenum);
                    cur = cur.Parent;
                }
            }
            catch (CommonMarkException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CommonMarkException("An error occurred while finalizing open containers.", cur, ex);
            }

            if (cur != cur.Top)
                throw new CommonMarkException("Unable to finalize open containers.", cur);

            try
            {
                BlockMethods.finalize(cur, linenum);
            }
            catch(CommonMarkException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new CommonMarkException("Unable to finalize document element.", cur, ex);
            }

            return cur;
        }

        /// <summary>
        /// Performs the second stage of the conversion - parses block element contents into inline elements.
        /// </summary>
        /// <param name="document">The top level document element.</param>
        /// <param name="settings">The object containing settings for the parsing process.</param>
        /// <exception cref="ArgumentException">when <paramref name="document"/> does not represent a top level document.</exception>
        /// <exception cref="ArgumentNullException">when <paramref name="document"/> is <c>null</c></exception>
        /// <exception cref="CommonMarkException">when errors occur during inline parsing.</exception>
        public static void ProcessStage2(Syntax.Block document, CommonMarkSettings settings = null)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            if (document.Tag != Syntax.BlockTag.Document)
                throw new ArgumentException("The block element passed to this method must represent a top level document.", "document");

            try
            {
                BlockMethods.ProcessInlines(document, document.ReferenceMap);
            }
            catch(CommonMarkException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new CommonMarkException("An error occurred during inline parsing.", ex);
            }
        }

        /// <summary>
        /// Performs the last stage of the conversion - converts the syntax tree to HTML representation.
        /// </summary>
        /// <param name="document">The top level document element.</param>
        /// <param name="target">The target text writer where the result will be written to.</param>
        /// <param name="settings">The object containing settings for the formatting process.</param>
        /// <exception cref="ArgumentException">when <paramref name="document"/> does not represent a top level document.</exception>
        /// <exception cref="ArgumentNullException">when <paramref name="document"/> or <paramref name="target"/> is <c>null</c></exception>
        /// <exception cref="CommonMarkException">when errors occur during formatting.</exception>
        /// <exception cref="IOException">when error occur while writing the data to the target.</exception>
        public static void ProcessStage3(Syntax.Block document, TextWriter target, CommonMarkSettings settings = null)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            if (target == null)
                throw new ArgumentNullException("target");

            if (document.Tag != Syntax.BlockTag.Document)
                throw new ArgumentException("The block element passed to this method must represent a top level document.", "document");

            if (settings == null)
                settings = CommonMarkSettings.Default;

            try
            {
                if (settings.OutputFormat == OutputFormat.SyntaxTree)
                {
                    Printer.PrintBlocks(target, document, 0);
                }
                else
                {
                    HtmlPrinter.BlocksToHtml(target, document, settings);
                }
            }
            catch (CommonMarkException)
            {
                throw;
            }
            catch(IOException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new CommonMarkException("An error occurred during formatting of the document.", ex);
            }
        }

        /// <summary>
        /// Converts the given source data and writes the result directly to the target.
        /// </summary>
        /// <param name="source">The reader that contains the source data.</param>
        /// <param name="target">The target text writer where the result will be written to.</param>
        /// <param name="settings">The object containing settings for the parsing and formatting process.</param>
        /// <exception cref="ArgumentNullException">when <paramref name="source"/> or <paramref name="target"/> is <c>null</c></exception>
        /// <exception cref="CommonMarkException">when errors occur during parsing or formatting.</exception>
        /// <exception cref="IOException">when error occur while reading or writing the data.</exception>
        public static void Convert(TextReader source, TextWriter target, CommonMarkSettings settings = null)
        {
            if (settings == null)
                settings = CommonMarkSettings.Default;

            var document = ProcessStage1(source, settings);
            ProcessStage2(document, settings);
            ProcessStage3(document, target, settings);
        }

        /// <summary>
        /// Converts the given source data and returns the result as a string.
        /// </summary>
        /// <param name="source">The source data.</param>
        /// <param name="settings">The object containing settings for the parsing and formatting process.</param>
        /// <exception cref="CommonMarkException">when errors occur during parsing or formatting.</exception>
        /// <returns>The converted data.</returns>
        public static string Convert(string source, CommonMarkSettings settings = null)
        {
            if (source == null)
                return null;

            using (var reader = new System.IO.StringReader(source))
            using (var writer = new System.IO.StringWriter(System.Globalization.CultureInfo.CurrentCulture))
            {
                Convert(reader, writer, settings);

                return writer.ToString();
            }
        }
    }
}
