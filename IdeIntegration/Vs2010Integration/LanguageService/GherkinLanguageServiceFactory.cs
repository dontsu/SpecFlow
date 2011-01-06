﻿using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace TechTalk.SpecFlow.Vs2010Integration.LanguageService
{
    public interface IGherkinLanguageServiceFactory
    {
        GherkinLanguageService GetLanguageService(ITextBuffer textBuffer);
    }

    [Export(typeof(IGherkinLanguageServiceFactory))]
    internal class GherkinLanguageServiceFactory : IGherkinLanguageServiceFactory
    {
        [Import]
        internal IProjectScopeFactory ProjectScopeFactory = null;

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        [Import]
        internal IVsEditorAdaptersFactoryService AdaptersFactory = null;

        public GherkinLanguageService GetLanguageService(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => CreateLanguageService(textBuffer));
        }

        private GherkinLanguageService CreateLanguageService(ITextBuffer textBuffer)
        {
            var project = VsxHelper.GetCurrentProject(textBuffer, AdaptersFactory, ServiceProvider);
            var projectScope = ProjectScopeFactory.GetProjectScope(project);
            var languageService = new GherkinLanguageService(projectScope);

            textBuffer.Changed +=
                (sender, args) => languageService.TextBufferChanged(GetTextBufferChange(args));

            languageService.TextBufferChanged(GetEntireBufferChange(textBuffer));

            return languageService;
        }

        private GherkinTextBufferChange GetEntireBufferChange(ITextBuffer textBuffer)
        {
            var textSnapshot = textBuffer.CurrentSnapshot;
            return new GherkinTextBufferChange(GherkinTextBufferChangeType.EntireFile, 
                0, textSnapshot.LineCount - 1, 0, textSnapshot.Length, 0, 0, textSnapshot);
        }

        private GherkinTextBufferChange GetTextBufferChange(TextContentChangedEventArgs textContentChangedEventArgs)
        {
            Debug.Assert(textContentChangedEventArgs.Changes.Count > 0);

            var startLine = int.MaxValue;
            var endLine = 0;
            var startPosition = int.MaxValue;
            var endPosition = 0;
            var lineCountDelta = 0;
            var positionDelta = 0;

            var beforeTextSnapshot = textContentChangedEventArgs.Before;
            var afterTextSnapshot = textContentChangedEventArgs.After;
            foreach (var change in textContentChangedEventArgs.Changes)
            {
                startLine = Math.Min(startLine, beforeTextSnapshot.GetLineNumberFromPosition(change.OldPosition));
                endLine = Math.Max(endLine, afterTextSnapshot.GetLineNumberFromPosition(change.NewEnd));

                startPosition = Math.Min(startPosition, change.OldPosition);
                endPosition = Math.Max(endPosition, change.NewEnd);
                lineCountDelta += change.LineCountDelta;
                positionDelta += change.Delta;
            }

            return new GherkinTextBufferChange(
                startLine == endLine ? GherkinTextBufferChangeType.SingleLine : GherkinTextBufferChangeType.MultiLine,
                startLine, endLine,
                startPosition, endPosition,
                lineCountDelta, positionDelta,
                afterTextSnapshot);
        }
    }
}