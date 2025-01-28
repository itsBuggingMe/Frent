namespace Frent.Components;

/*  ALL COMPONENT TYPES                                 |Interface|Storage
 *  Arbitary data                                           X       X
 *  Update Only                                             X       X
 *  Update with N components                                X       X
 *  Update with N components + uniform                      X       X
 *  Update with N components + entityid                     X       X
 *  Update with N components + uniform + entityid           X       X
 *  Update with uniform                                     X       X
 *  Update with entityid                                    X       X
 *  Update with uniform + entityid                          X       X
 */

/// <summary>
/// Base marker component for all component interfaces
/// </summary>
/// <remarks>All components with <see cref="IComponentBase"/> will be auto-registered. This makes it useful for AOT compilation scenarios</remarks>
public interface IComponentBase;