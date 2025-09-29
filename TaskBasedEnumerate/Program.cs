// See https://aka.ms/new-console-template for more information

using TaskBasedEnumerate;

Console.WriteLine("Hello, task-based enumeration!");

var oneThroughTen = IEnumerable<int>.Produce(async ctx =>
{
    await ctx.Yield(1);
    await ctx.Yield(2);
    await ctx.Yield(3);
    await ctx.Yield(4);
    await ctx.Yield(5);
    await ctx.Yield(6);
    await ctx.Yield(7);
    await ctx.Yield(8);
    await ctx.Yield(9);
    await ctx.Yield(10);
});

foreach (var i in oneThroughTen)
{
    Console.WriteLine(i);
}
Console.WriteLine();

foreach (var _ in IEnumerable<int>.Produce(_ => Task.CompletedTask))
{
    Console.WriteLine("This will never happen.");
}

var fibs = IEnumerable<int>.Produce(async ctx =>
    {
        var prev = 0;
        var next = 1;

        await ctx.Yield(prev);
        await ctx.Yield(next);

        while (true)
        {
            var fib = prev + next;
            prev = next;
            next = fib;

            await ctx.Yield(next);
        }
        // ReSharper disable once FunctionNeverReturns
    }
);

foreach (var fib in fibs.TakeWhile(fib => fib < 1000))
{
    Console.WriteLine(fib);
}

