using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Events.Handlers
{
    /// <summary>
    /// 同步事件处理器委托
    /// </summary>
    /// <typeparam name="T">事件类型（必须是struct）</typeparam>
    /// <param name="evt">事件数据引用（可修改）</param>
    /// <example>
    /// <code>
    /// EventCallback&lt;PlayerDamageEvent&gt; handler = (ref PlayerDamageEvent evt) => {
    ///     Debug.Log($"受到 {evt.Damage} 点伤害");
    ///     evt.Damage = 0;  // 可以修改事件数据
    /// };
    /// </code>
    /// </example>
    public delegate void EventCallback<T>(ref T evt) where T : struct;

    /// <summary>
    /// 异步事件处理器委托
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="evt">事件数据（值传递）</param>
    /// <returns>UniTask</returns>
    /// <example>
    /// <code>
    /// AsyncEventHandler&lt;DataLoadEvent&gt; handler = async evt => {
    ///     await LoadDataAsync(evt.Path);
    /// };
    /// </code>
    /// </example>
    public delegate UniTask AsyncEventHandler<T>(T evt) where T : struct;

    /// <summary>
    /// 带返回值的查询处理器委托
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="evt">事件数据引用</param>
    /// <returns>处理结果</returns>
    /// <example>
    /// <code>
    /// QueryHandler&lt;DamageCalcEvent, int&gt; handler = (ref DamageCalcEvent evt) => {
    ///     return evt.BaseDamage * 2;
    /// };
    /// </code>
    /// </example>
    public delegate TResult QueryHandler<T, out TResult>(ref T evt) where T : struct;

    /// <summary>
    /// 事件初始化器委托（用于struct事件的初始化）
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="evt">事件数据引用</param>
    /// <example>
    /// <code>
    /// dispatcher.Dispatch&lt;PlayerDamageEvent&gt;(ref evt => {
    ///     evt.Damage = 10;
    ///     evt.Source = "Enemy";
    /// });
    /// </code>
    /// </example>
    public delegate void EventInitializer<T>(ref T evt) where T : struct;

    /// <summary>
    /// 事件过滤器委托
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="evt">事件数据引用</param>
    /// <returns>true表示通过过滤，false表示过滤掉</returns>
    /// <example>
    /// <code>
    /// dispatcher.Subscribe&lt;PlayerDamageEvent&gt;(handler)
    ///     .Where((ref PlayerDamageEvent evt) => evt.Damage > 10);  // 只处理伤害大于10的事件
    /// </code>
    /// </example>
    public delegate bool EventFilter<T>(ref T evt) where T : struct;
}
