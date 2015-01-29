# Text Filtering Pipeline

(Note: be sure to read the "History and Context" section at the end for more information about why this project was created, and what it means for you.)

TFP is a C# pipeline processor for text, intended for editorial usage through configuration by simple text commands. A pipeline is a series of filters, processed in sequential order.  In most cases, the output from one filter is the input to the filter immediately following it (this is the "active text").

The filters are linear and sequential.  Text is passed "down the line," and is usually modified during each step, coming out the other end in a different form than when it started.

## The Basics

Pretend for a moment that we want to add our name to some text and then format that for HTML. We can create these commands:

    Prepend "My name is: "
    Append "."
    Html.Wrap p

What this says, in order, is:

1. Put the text "My name is: " before the input (what we pass into the pipeline)
2. Put a period after the input
3. Wrap the result in P tag
4. (Implied) Return the result -- the pipeline returns the result of the last operation.

Now, if we pass "Deane" to the pipeline, it comes out:

    <p>My name is Deane.</p>

If we immediately passed "Annie" to the pipeline, we'd get...

    <p>My name is Annie.</p>

Additionally, some pipeline commands can obtain text in-process.  For instance, if we wanted to format and output the contents of a file on the file system, we could do something like this:

    File.Read my-file.txt
    Replace foo bar
    Format "The contents of the file are: {0}."
    Html.Wrap p

That would read in the contents of "my-file.txt," replace the string "foo" with "bar," drop the result into the middle of a sentence, and again wrap it in a P tag.  In this case we don't pass anything into the pipeline -- it obtains text to work with in the first step.

Filters are grouped into categories which do different things.  For example, the "HTTP" category can make web requests, and the "HTML" category can manipulate HTML documents.  Combine them, and you can do things like this:

    Http.Get gadgetopia.com
    Html.Extract //title
    Format "The title of this web page is {0}."

Http.Get makes a -- wait for it -- GET request over HTTP to the URL specified in the first argument and returns the HTML. Html.Extract uses an external library to reach into the HTML and grab a value.  Format, as we saw before, wraps this value within other text.

(See "Variables" below for a more extensive and practical example of working over HTTP.)

There are two programming "levels" to this library.

* There's the C# level, which instatiates the pipeline, passes data to it, and does something with the result. This tends to be fairly static -- it will be implemented once in a way to make it available for editors (those using a CMS, for example -- this was the original intent; see "History and Context" at the end of this document).
* Then there's the filter configuration level, which involves setting up the filters and telling them what to do.  This level requires (1) knowing the format for calling filters and passing arguments; and (2) knowing what filters are available, what information they need, and what results they will return.

The first level is intended for C# developers.  The second level is intended for non-developers -- primarily content editors that need to obtain and modify text-based content for publication, without the assistance of a developer.

## The <span>C#</span>

(Note: The words "command" and "filter" get used interchangably in this document. Technically, a "command" is an object that invokes and configures a "filter," which is a method. In practice, I'll go back and forth between the terms indiscriminately. Sorry.)

Here's the C# to instantiate the pipeline and add a command, the long way.

    var pipeline = new TextFilterPipeline();
    pipeline.AddCommand(
       new TextFilterCommand()
       {
         CommandName = "Prepend",
         CommandArgs = new Dictionary<object,string>() { { 1,"FOO" } }
       }
      );
    var result = pipeline.Execute("BAR");
    // Result contains "FOOBAR"

Clearly, this is way too verbose.  So commands can be added by simple text strings.  The strings are tokenized on whitespace. The first token is the command name, the subsequent tokens are arguments. (Any arguments which contain whitespace need to be in quotes.)

    var pipeline = new TextFilterPipeline();
    pipeline.AddCommand("Prepend FOO");  //Note: this can also be passed into the constructor
    var result = pipeline.Execute("BAR");

The result will be "FOOBAR".

The pipeline remains "loaded" with commands, so we could just as easily do this immediately after:

    pipeline.Execute("BAZ");

We'd get "FOOBAZ."  We could pass a thousand different strings to the pipeline, and they would all come out with "FOO" prepended to them.

"Prepend" is one example of several dozen pre-built filters. Some take arguments, some don't. It's up to the individual filter how many arguments it needs, what order it needs them in, and what it does with them during execution (much like function calls in any programming language).

Commands can be passed in _en masse_, separated by line breaks (note that command parsing is broken out to its own class, and could easily be re-implemented, if you wanted to do something different).  Each line is parsed as a separate command.

    var pipeline = new TextFilterPipeline(thousandsAndThousandsOfCommands);

The pipeline doesn't technically have to even start with text, as some filters allow the pipeline to acquire text mid-stream. In these cases,  the pipeline is invoked without arguments.

    pipeline.Execute();
    
How a filter "treats" the active text is up to the filter. _The active text becomes whatever the filter returns._  It can return some derivation of the input text (such as with "Prepend," from above), or it can return something completely new without regard to the active text it took in. It can even use the active text to configure itself and then return something else. (Example: if invoked without arguments, Http.Get assumes the active text is the URL it should use. It retrieves the HTML at that URL, and returns the result.)

In our example above, after the first filter (Http.Get) executes, the active text is _all_ the HTML from the home page of Gadgetopia.  After the second filter (Html.Extract) executes, the active text is just the contents of the "title" tag.  The active text after the last filter executes is what is returned by the Execute method of the pipeline object (it's what comes out "the other end" of the pipe).

Filters are grouped into categories (think "namespaces").  Any command without a "dot" is assumed to map to "Core" category.

## Variables

By default, a filter changes the active text and passes it to the next filter. However, the result of a filter can be instead redirected into variable which is stored for later use.  This does _not_ change the active text -- it remains unchanged.

You can direct the result of an operation to a variable by using the "=>" operator and a variable name at the end of a statement.  Variable names start with a dollar sign ("$").

Here's an example of chaining filters and writing into and out of variables to obtain and format the temperature in Sioux Falls:

    Http.Get http://api.openweathermap.org/data/2.5/weather?q=Sioux+Falls&mode=xml&units=imperial
    Xml.Extract //city/@name => $city
    Xml.Extract //temperature/@value => $temp
    Format "The temp in {city} is {temp}."
    Html.Wrap p weather-data

The first command gets an XML document. Since the second command sends the results to a variable named $city, the active text remains the original full XML document which is then still available to the third command.

(Note that in this case, that XML document is going to be fully parsed twice from the string source, which may or may not work for your situation, performance-wise. Remember that filters only pass simple text, not more complex objects.)

The result of this pipeline is:

    <p class="weather-data">The temp in Sioux Falls is 37.</p>

Variables are volatile -- writing to the same variable multiple times simply resets the value each time.

Trying to retrieve a variable before it exists will result in an error.  To initialize variables to avoid this, use InitVar:

    InitVar $myVar $myOtherVar

To manually set a variable value, use SetVar.

    SetVar $name Deane

This sets the value of $name to "Deane."

## Extending Filters

Filters are pluggable. Simply write a static class and method, like this:

    [TextFilters("Text")]
    public static class TextFilters
    {
      [TextFilter("Left")]
      public static string Left(string input, TextFilterCommand command)
      {
        var length = int.Parse(command.CommandArgs[0]);
        return input.Substring(0, length);
      }
    }

Then register this with the pipeline:

    TextFilterPipeline.AddType(typeof(TextFilters));

After registering, our command is now available as:

    Text.Left 10

You register an entire type at a time, not individual methods; only methods with the "TextFilter" attribute will actually get added. You can even do entire assemblies, if all your filters are in a separate DLL:

    var myAssembly = Assembly.LoadFile(@"C:\MyFilters.dll");
    TextFilterPipeline.AddAssembly(myAssembly);

That will find all the types marked with the "TextFilters" attribute, and -- within those types -- find all methods with the "TextFilter" attribute.

Note that the name of the underlying C# method is irrelevant.  The filter maps to the combination of the category name ("Text," in this case) and filter ("Left"), both supplied by the attributes. While it would make sense to call the method the same name as the filter, this isn't required.

If your category and command name are identical to another one, the last one in wins. This means you can "hide" previous filters by registering new ones that take their place.  New filters are loaded statically, so they're globally available to all executions of the pipeline.

In the example above case, we're trusting that this filter will be called with (1) at least one argument (any extra arguments are simply ignored), (2) that the argument will parse to an Int32, and (3) that the numeric value isn't longer than the active text.  Clearly, _you're gonna want to validate and error check this inside your filter before doing anything_.

And what happens if there's an error condition?  Do you return the string unchanged?  Do you throw an exception?  That's up to you, but there is no user interaction during pipeline execution, so error conditions are problematic.

You can map the same filter to multiple command names, then use that name inside the method to change execution.

    [TextFilters("Text")]
    public static class TextFilters
    {
      [TextFilter("Left")]
      [TextFilter("Right")]
      public static string Left(string input, TextFilterCommand command)
      {
        var length = int.Parse(command.CommandArgs[0]);

        if(command.CommandName == "Left")
        {
          return input.Substring(0, length);
        }
        else
        {
          return input.Substring(input.Length - length);
        }
      }
    }

This filter will map to both of these commands:

    Text.Left 10
    Text.Right 10

Note that this is true even though the method name ("Left") did not change.

## Contents

This repo contains three projects.

1. The source to create a DLL named "BlendInteractive.TextFilterPipeline.dll"
2. A test project with moderate unit test coverage (needs to be better)
3. A WinForms testing app which provides a GUI to run and test filters.

On build, the DLL, a supporting DLL (HtmlAgilityPack), and the WinForms EXE are copied into the "Binaries" folder. The WinForms tester should run directly from there.

## History and Context

This started out as a simple project to allow editors to include the contents of a text file within [EPiServer](http://episerver.com) content.

That CMS provides "blocks," which are reusable content elements.  I wrote a simple block into which a editor could specify the path to a file on the file system. The block would read the contents of the file and dump it into the page.  It was essentially a server-side file include for content editors.

Then I got to thinking (always dangerous) that some files might need to have newlines replaced with BR tags, and how would I specify that?  And what if the file wasn't local?  How would I specify a remote file?  And what if it was XML -- could I specify a transform?

And the idea of a text filter pipeline was born.  To support this, I needed to come up with language constructs, and that's when I started parsing commands. And then I found I could enable some really neat functionality by tweaking and tuning and making small changes.

And when the snowball finally came to a rest at the bottom of the hill, you had, well, this.

The constant challenge with this type of project is knowing when to stop. At what point are you simply inventing a new programming language?  When do you cross the line from simple and useful to pointless and redundant?  And when do you cross another line into something which is potentially dangerous in the hands of non-programmers?

Look back to the weather example from above -- that really has nothing to do with text filtering. The pipeline is executed without input, the XML is obtained in the first step, and content is extracted then formatted. In this case, we're not filtering at all. We're really edging into a simplistic procedural programming language. How far is too far?  At what point does [Alan Turing roll over in his grave](http://stackoverflow.com/questions/7284/what-is-turing-complete)?

I don't have an answer for that (hell, we may have crossed the line already).  I leave it to you to judge.

Implement with care. Happy Filtering.

Deane Barker, January 2015
