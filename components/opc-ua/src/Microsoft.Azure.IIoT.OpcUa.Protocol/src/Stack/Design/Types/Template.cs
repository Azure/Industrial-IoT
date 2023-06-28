/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;

namespace Opc.Ua.Design.Schema {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Generates types used to implement an address space.
    /// </summary>
    public class Template {

        /// <summary>
        /// Initializes the stream from the resource block of the specified assembly.
        /// </summary>
        public Template(TextWriter writer, string templatePath, Assembly assembly) :
            this(writer, false, templatePath, assembly) {
        }

        /// <summary>
        /// Initializes the stream from the resource block of the specified assembly.
        /// </summary>
        private Template(TextWriter writer, bool written, string templatePath, Assembly assembly) {
            Initialize();

            try {
                var names = assembly.GetManifestResourceNames();

                if (templatePath.Length > 0) {
                    _reader = new StreamReader(assembly.GetManifestResourceStream(templatePath));
                }
                else {
                    _reader = new StringReader(string.Empty);
                }
            }
            catch (Exception e) {
                throw new ApplicationException(string.Format("Template '{0}' not found.", templatePath), e);
            }

            _writer = writer;
            _written = written;
            _assembly = assembly;
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize() {
            Replacements = new Dictionary<string, string>();
            Templates = new Dictionary<string, TemplateDefinition>();
            _reader = null;
            _writer = null;
            IndentCount = 0;
            TemplateStartTag = "***START***";
            TemplateEndTag = "***END***";
            TemplateEndAppendNewLineTag = "***ENDAPPENDNEWLINE***";
        }

        /// <summary>
        /// The tag that marks the start of a template body.
        /// </summary>
        protected string TemplateStartTag { get; set; }

        /// <summary>
        /// The tag that marks the end of a template body.
        /// </summary>
        protected string TemplateEndTag { get; set; }

        /// <summary>
        /// The tag that marks the end of a template body and is replaced by a new line.
        /// </summary>
        protected string TemplateEndAppendNewLineTag { get; set; }

        /// <summary>
        /// The number of levels to ident a the current line.
        /// </summary>
        protected int IndentCount { get; set; }

        /// <summary>
        /// Returns enough whitespace to indent the current line properly.
        /// </summary>
        public string Indent {
            get {
                if (IndentCount > 0) {
                    return new string(' ', IndentCount * 4);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the new line characters.
        /// </summary>
        public string NewLine => "\r\n";

        /// <summary>
        /// The table of tokens to replace.
        /// </summary>
        public Dictionary<string, string> Replacements { get; private set; }

        /// <summary>
        /// The templates to load.
        /// </summary>
        public Dictionary<string, TemplateDefinition> Templates { get; private set; }

        /// <summary>
        /// Adds a replacement value for a token.
        /// </summary>
        public void AddReplacement(string token, object replacement) {
            if (replacement is bool) {
                Replacements.Add(token, ((bool)replacement) ? "true" : "false");
            }
            else {
                Replacements.Add(token, string.Format("{0}", replacement));
            }
        }

        /// <summary>
        /// Performs the substitutions specified in the template and writes it to the stream.
        /// </summary>
        public bool WriteTemplate(GeneratorContext context) {
            // ensure context is not null.
            if (context == null) {
                context = new GeneratorContext();
            }

            var skipping = true;
            var written = false;

            // build list of tokens.
            var count = 0;

            var tokens = new string[Replacements.Count];

            foreach (var token in Replacements.Keys) {
                tokens[count++] = token;
            }

            // read first line.
            var line = _reader.ReadLine();

            while (line != null) {
                // if skipping lines look for the template start tag.
                if (skipping) {
                    if (line.IndexOf(TemplateStartTag, StringComparison.Ordinal) != -1) {
                        skipping = false;
                    }

                    line = _reader.ReadLine();
                    continue;
                }

                // if writing lines look for the template end tag.
                else {
                    if (line.IndexOf(TemplateEndAppendNewLineTag, StringComparison.Ordinal) != -1) {
                        Write(NewLine);
                        break;
                    }
                    if (line.IndexOf(TemplateEndTag, StringComparison.Ordinal) != -1) {
                        break;
                    }
                }

                // process empty lines.
                if (line.Length == 0) {
                    if (written) {
                        Write(NewLine);
                    }

                    written = true;
                    line = _reader.ReadLine();
                    continue;
                }

                var found = false;

                for (var index = 0; index < line.Length; index++) {
                    // check for a token at the current position.
                    string token = null;

                    for (var ii = 0; ii < tokens.Length; ii++) {
                        if (StrCmp(line, index, tokens[ii])) {
                            token = tokens[ii];
                            break;
                        }
                    }

                    // nothing found.
                    if (token == null) {
                        continue;
                    }

                    // check if a template substitution is required.
                    if (Templates.ContainsKey(token)) {
                        // skip the token if no items to write.
                        var definition = Templates[token];

                        if (definition == null || definition.Targets == null || definition.Targets.Count == 0) {
                            found = true;
                            line = line.Substring(index + token.Length);
                            index = -1;
                            continue;
                        }

                        // write multi-line template.
                        var result = WriteTemplate(
                            context.Target,
                            token,
                            string.Concat(context.Prefix, line.AsSpan(0, index)));

                        if (result) {
                            written = true;
                        }

                        line = string.Empty;
                        continue;
                    }

                    // only process tokens if a value is provided.
                    if (Replacements[token] != null) {
                        written = WriteToken(
                            context.Target,
                            context,
                            !found,
                            line.Substring(0, index),
                            token);

                        found = true;
                    }

                    line = line.Substring(index + token.Length);
                    index = -1;
                }

                // write line if no token found.
                if (line.Length > 0) {
                    if (!found) {
                        // ensure that an empty line does not get inserted at the start of a file.
                        if (written || context.Target != null) {
                            Write(NewLine);
                        }

                        Write(context.Prefix);
                        written = true;
                    }

                    Write(line);
                }

                // read next line.
                line = _reader.ReadLine();
            }

            return written;
        }

        /// <summary>
        /// Writes the text to the stream.
        /// </summary>
        public void Write(char text) {
            _writer.Write(text);
            _written = true;
        }

        /// <summary>
        /// Writes the text to the stream.
        /// </summary>
        public void Write(string text) {
            if (!_written) {
                if (text == NewLine) {
                    return;
                }
            }

            _writer.Write(text);
            _written = true;
        }

        /// <summary>
        /// Formats and then writes the text to the stream.
        /// </summary>
        public void Write(string format, object arg1) {
            _writer.Write(format, arg1);
            _written = true;
        }

        /// <summary>
        /// Formats and then writes the text to the stream.
        /// </summary>
        public void Write(string format, object arg1, object arg2) {
            _writer.Write(format, arg1, arg2);
            _written = true;
        }

        /// <summary>
        /// Formats and then writes the text to the stream.
        /// </summary>
        public void Write(string format, object arg1, object arg2, object arg3) {
            _writer.Write(format, arg1, arg2, arg3);
            _written = true;
        }

        /// <summary>
        /// Formats and then writes the text to the stream.
        /// </summary>
        public void Write(string format, params object[] args) {
            _writer.Write(format, args);
            _written = true;
        }

        /// <summary>
        /// Writes a newline and then indents the text for the next line.
        /// </summary>
        public void WriteNextLine(string prefix) {
            _writer.Write(NewLine);
            _writer.Write(Indent);
            _writer.Write(prefix);
            _written = true;
        }

        /// <summary>
        /// Writes the text to the stream followed by a new line.
        /// </summary>
        public void WriteLine(string text) {
            _writer.Write(Indent);
            _writer.Write(text);
            _writer.Write(NewLine);
            _written = true;
        }

        /// <summary>
        /// Formats and then writes the text to the stream followed by a new line.
        /// </summary>
        public void WriteLine(string text, object arg1) {
            WriteLine(text, new object[] { arg1 });
        }

        /// <summary>
        /// Formats and then writes the text to the stream followed by a new line.
        /// </summary>
        public void WriteLine(string text, object arg1, object arg2) {
            WriteLine(text, new object[] { arg1, arg2 });
        }

        /// <summary>
        /// Formats and then writes the text to the stream followed by a new line.
        /// </summary>
        public void WriteLine(string text, object arg1, object arg2, object arg3) {
            WriteLine(text, new object[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Formats and then writes the text to the stream followed by a new line.
        /// </summary>
        public void WriteLine(string text, object[] args) {
            _writer.Write(Indent);
            _writer.Write(text, args);
            _writer.Write(NewLine);
            _written = true;
        }



        /// <summary>
        /// Substitutes simple text template for a token.
        /// </summary>
        protected virtual bool WriteToken(
            object target,
            GeneratorContext context,
            bool firstToken,
            string prefix,
            string token) {
            // write context prefix for first token.
            if (firstToken) {
                Write(NewLine);
                Write(context.Prefix);
            }

            // write prefix.
            Write(prefix);

            // write replacement.
            var replacement = Replacements[token];

            if (replacement != null) {
                Write(replacement);
            }

            return true;
        }

        /// <summary>
        /// Substitutes a multi-line template for a token.
        /// </summary>
        protected virtual bool WriteTemplate(
            object container,
            string token,
            string prefix) {
            var written = false;

            // write each item in the list.
            var context = new GeneratorContext {
                Container = container,
                Token = token,
                Index = 0,
                FirstInList = true,
                Prefix = prefix
            };

            var definition = Templates[token];

            context.TemplatePath = definition.TemplatePath;

            foreach (var target in definition.Targets) {
                context.Target = target;

                // get the template path name.
                var templatePath = definition.Load(this, context);

                // skip item if no template specified.
                if (templatePath == null) {
                    context.Index++;
                    continue;
                }

                // load the template.
                var template = new Template(_writer, _written, templatePath, _assembly);

                if (template != null) {
                    if (!context.FirstInList && context.BlankLine) {
                        Write(NewLine);
                    }

                    if (definition.Write(template, context)) {
                        context.FirstInList = false;
                        written = true;
                    }

                    _written = template._written;
                }

                context.Index++;
            }

            // return flag indicating whether something was written.
            return written;
        }

        /// <summary>
        /// Determines if the target exists in the string at the specified index.
        /// </summary>
        protected bool StrCmp(string source, int index, string target) {
            for (var ii = 0; ii < target.Length; ii++) {
                if (index + ii >= source.Length || source[index + ii] != target[ii]) {
                    return false;
                }
            }

            return true;
        }

        private TextReader _reader;
        private TextWriter _writer;
        private readonly Assembly _assembly;
        private bool _written;

    }

    /// <summary>
    /// A delegate handle events associated with template.
    /// </summary>
    public delegate string LoadTemplateEventHandler(Template template, GeneratorContext context);

    /// <summary>
    /// A delegate handle events associated with template.
    /// </summary>
    public delegate bool WriteTemplateEventHandler(Template template, GeneratorContext context);
}
