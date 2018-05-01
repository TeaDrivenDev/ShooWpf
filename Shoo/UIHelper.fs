namespace Shoo.UIHelper

open System
open System.Windows
open System.Windows.Data

type BytesToMegabytesConverter() =
    static member Instance = BytesToMegabytesConverter() :> IValueConverter

    interface IValueConverter with
        member this.Convert(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            if value = DependencyProperty.UnsetValue
            then 0.
            else (System.Convert.ToDouble value) / 1_000_000.
            :> obj

        member this.ConvertBack(value: obj, targetType: Type, parameter: obj, culture: Globalization.CultureInfo): obj =
            raise (System.NotImplementedException())
