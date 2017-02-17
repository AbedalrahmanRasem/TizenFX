﻿/*
 * Copyright (c) 2017 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Tizen.WebView
{
    public static class Chromium
    {
        /// <summary>
        /// Initializes Chromium's instance
        /// </summary>
        /// <returns>A reference count of Chromium's instance</returns>
        public static int Initialize()
        {
            return Interop.ChromiumEwk.ewk_init();
        }

        /// <summary>
        /// Decreases a reference count of WebKit's instance, possibly destroying it
        /// </summary>
        /// <returns>A reference count of Chromium's instance</returns>
        public static int Shutdown()
        {
            return Interop.ChromiumEwk.ewk_shutdown();
        }
    }
}
