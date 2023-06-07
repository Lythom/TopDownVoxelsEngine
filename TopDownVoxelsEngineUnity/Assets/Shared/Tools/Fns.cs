namespace StarTeam.Tools {
    public static class Fns {
        public static (T0, T1) Tuplify<T0, T1>(T0 arg0, T1 arg1) => (arg0, arg1);
        public static (T0, T1, T2) Tuplify<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2) => (arg0, arg1, arg2);
        public static (T0, T1, T2, T3) Tuplify<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3) => (arg0, arg1, arg2, arg3);
        public static (T0, T1, T2, T3, T4) Tuplify<T0, T1, T2, T3, T4>(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (arg0, arg1, arg2, arg3, arg4);

        public static (T0, T1, T2, T3, T4, T5) Tuplify<T0, T1, T2, T3, T4, T5>(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            (arg0, arg1, arg2, arg3, arg4, arg5);

        public static (T0, T1, T2, T3, T4, T5, T6) Tuplify<T0, T1, T2, T3, T4, T5, T6>(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            (arg0, arg1, arg2, arg3, arg4, arg5, arg6);

        public static (T0, T1, T2, T3, T4, T5, T6, T7) Tuplify<T0, T1, T2, T3, T4, T5, T6, T7>(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) =>
            (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        public static (T0, T1, T2, T3, T4, T5, T6, T7, T8) Tuplify<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) =>
            (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
}