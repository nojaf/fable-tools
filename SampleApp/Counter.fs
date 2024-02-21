module Counter

type CounterProps =
    {|
        name : string
        lastName : string option
    |}

let Counter (props : CounterProps) = 0