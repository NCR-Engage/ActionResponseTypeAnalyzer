# ActionResponseTypeAnalyzer

[![Build status](https://ci.appveyor.com/api/projects/status/hmxmmk7wi4ibmk5e/branch/master?svg=true)](https://ci.appveyor.com/project/NCREngage/actionresponsetypeanalyzer/branch/master)

## Problem

WebApi's HttpResponseMessage is not a generic type. It was at the beginning, but before the release it was decided that it should not be and so we stuck with non-descriptive action method calls like

```
public HttpResponseMessage Get([FromUri] int someId) { ... }
```

Creators of WebApi documentation needed to know what the type is, so they invented ``System.Web.Http.Description.ResponseTypeAttribute``. So now there are two sources of truth about what the method returns - method body decides what is being run by computers and attribute decides whay is being read by humans. This leads to mismatches.

## Solution

This analyzer is a patch that compares actual types with the declared ones. It contains two diagnostics:

* ARTA001: Value type declared in ResponseType should match to actual response type.
* ARTA002: Public controller method specify their response type in the ResponseType attribute.
