/*
 * Copyright(c) 2022 Samsung Electronics Co., Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.ComponentModel;

namespace Tizen.NUI.Accessibility
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAtspiEditableText : IAtspiText
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool AccessibilityCopyText(int startPosition, int endPosition);

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool AccessibilityCutText(int startPosition, int endPosition);

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool AccessibilityInsertText(int startPosition, string text);

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool AccessibilitySetTextContents(string newContents);

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool AccessibilityDeleteText(int startPosition, int endPosition);
    }
}
