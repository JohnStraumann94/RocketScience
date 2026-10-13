## Background

The bi-elliptic transfer uses three burns and two transfer ellipses. The vehicle first burns
to a very high intermediate apoapsis r_b, performs a shape-change burn there, then burns into
the final orbit at r2. The Oberth effect is the reason this can beat a Hohmann: a burn's
effect on specific orbital energy is proportional to the speed at which it is performed. At a
high apoapsis the vehicle is moving slowly, so the shape-change burn there is cheap in delta-v
terms, even though it covers a large geometric angle.

The break-even radius ratio is about 11.94. Below it, Hohmann always wins regardless of where
the intermediate apoapsis is placed. Above about 15.58 the bi-elliptic always wins, given a
sufficiently distant r_b. In between, the bi-elliptic can win or lose depending on r_b, and
the price is always flight time: two full semi-ellipses instead of one, often taking many
times longer.

The defaults (r1 = 7,000 km, rb = 210,000 km, r2 = 140,000 km) give a radius ratio of 20,
well above the break-even point, so the bi-elliptic should win by a visible margin.

## Experiments to try

1. Run with the defaults and check the verdict. The bi-elliptic should save a fraction of a
   km/s at the cost of several extra days of flight time.

2. Reduce `rb` to something only slightly above `r2`, say 145000. The intermediate apoapsis
   is barely higher than the destination, and the second burn approaches zero. Watch the
   bi-elliptic total rise toward the Hohmann total — at the limit they converge.

3. Set `r2` to 83580 so that r2/r1 ≈ 11.94, the break-even ratio. Hohmann and bi-elliptic
   totals should be nearly equal. Notice that any `rb` you choose gives approximately the same
   bi-elliptic cost here.

4. Change `r2` to 42164 (GEO) with `r1` = 6678. The ratio is about 6.3, below break-even.
   The Hohmann should win regardless of where `rb` is placed. Confirm by pushing `rb` to
   500000 — the bi-elliptic still loses.

5. Push `rb` to 2000000 (roughly 2 million km) with the default r1 and r2. The intermediate
   apoapsis is almost at the edge of Earth's sphere of influence. The saving is real but the
   flight time becomes impractical — read off the days in the output to see how much.

## Watch out for

- **`rb` must be strictly greater than both `r1` and `r2`.** It is the turning point — the
  apex of both transfer ellipses. The sample will reject any `rb` that violates this.

- **The three-burn total can exceed the Hohmann total.** This is not a bug; it is the physics.
  Below the break-even radius ratio, pushing the intermediate apoapsis higher makes the
  bi-elliptic *more* expensive, not less. The savings only materialise above the break-even
  ratio.

- **Flight time is the hidden cost.** A bi-elliptic transfer with a very high `rb` can easily
  take days or weeks longer than the Hohmann. For missions with limited on-board power or
  consumables, the delta-v saving is not always worth it.

> The break-even ratio of 11.94 is a single number derived analytically in the book. It is
> independent of which specific orbits are involved — only the *ratio* r2/r1 matters, not the
> absolute values. This is a consequence of the vis-viva equation being scale-invariant when
> all distances are measured in the same units.
