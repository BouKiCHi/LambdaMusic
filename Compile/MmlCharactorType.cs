namespace LambdaMusic.Compile {
    enum MmlCharactorType {
        /// <summary>
        /// 空白文字
        /// </summary>
        Space,

        /// <summary>
        /// セパレータ(カンマ)
        /// </summary>
        Separator,

        /// <summary>
        /// 改行
        /// </summary>
        NextLine,

        /// <summary>
        /// ファイル終端
        /// </summary>
        Eof,

        /// <summary>
        /// 一行コメント
        /// </summary>
        CommentLine,

        /// <summary>
        /// コメントブロック開始
        /// </summary>
        CommentStart,
        /// <summary>
        /// コメントブロック終了
        /// </summary>
        CommentEnd,

        /// <summary>
        /// 一般文字
        /// </summary>
        GeneralChanacter,

        /// <summary>
        /// ブロック開始
        /// </summary>
        BlockStart,

        /// <summary>
        /// ブロック終了
        /// </summary>
        BlockEnd
    }
}
