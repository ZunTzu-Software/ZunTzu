HOW TO BUILD ZunTzu.exe
==========================

Install Visual Studio Community 2022.
Make sure the Desktop development with C++ workload is selected.
In the Individual components tab, under Compilers, build tools, and runtimes, choose C++ Windows XP Support for VS 2017 (v141) tools [Deprecated].

NOTES
=====

Localization will not work in Debug mode due to a bug in Visual Studio.
The bug was supposedly fixed in Visual Studio 2017 Update 15.4, but seems to have reappeared in a later version (see https://developercommunity.visualstudio.com/t/uwp-resx-localization-does-not-work-in-vs-2017/35699?viewtype=all).