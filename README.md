# Glitter
Glitter is a toy language with delusions of grandeur. It takes inspiration from expressive modern languages including
JavaScript and Typescript and bring them to a system language. Glitter's implementation is designed to be simple enough
for anyone to contribute and fast enough to be useful.

# Current status
The project is in a very early phase of implementation. Right now a simple AST interpreter is being written to explore
language design. Once the language design is finalized the interpreter will be ported to C++, and a native compiler 
will be built.

# Language
Glitter looks like this:

```
// Say Hello World!
sayHello("World");

// Bind values to names with let statements.
let name = "Glitter programming language 🎉🎉🎉";		// UTF-8 Strings are first class citizens.
let version = 1;										// Integers and floating point are supported.
let isAwesome = true;									// Booleans too!
let aSillyArray = [1, 2, 3, 4, 5];						// Arrays are as simple as you would expect.

// Expressions can perform more complex calculations.
let foobar = (84 / 2) + 5 * 2;

// Define a function that takes a name and prints hello to that name.
function sayHello(a)
{
	let what = "Hello " + a + "!";
	println(what);
}
```

Functions and recursion

```
// Define a fibonacci function with recursion.
function fibonacci(n)
{
	if (n <= 1)
	{
		return n;
	}
	else
	{
		// Recursion, yay!
		return fibonacci(n - 2) + fibonacci(n - 1);
	}
}

// Use a traditional for loop to print the first 10 fibonacci numbers.
for (var i = 0; i < 10; i = i + 1)
{
	print("fib " + i + ": " + fibonacci(i));
}
```

Glitter supports function literals and closures as first class citizens:
```
function makeCounter()
{
	var counter = 0;

	function count()
	{
		counter = counter + 1;
		print counter;
	}

	return c;
}

var a = makeCounter();
a();			// "1"
a();			// "2"

var b = makeCounter();
b();			// "1"
```

TODO: Classes
TODO: Type hinting



# Future
The language, interpreter and compiler are in development. Among the many things that will be added are:
 - classes
 - arrays, strings, tables (hashes)
 - typing (language is gradually typed)
 - c++ interpreter and compiler once prototype is finished
 - assembly output for x86